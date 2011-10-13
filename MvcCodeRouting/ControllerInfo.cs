﻿// Copyright 2011 Max Toro Q.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Web.Mvc;

namespace MvcCodeRouting {
   
   [DebuggerDisplay("{ControllerUrl}")]
   class ControllerInfo {

      static readonly Type baseType = typeof(ControllerBase);
      const string RootController = "Home";
      const string DefaultAction = "Index";

      readonly CodeRoutingSettings settings;
      readonly string rootNamespace;

      ReadOnlyCollection<string> _NamespaceRouteParts;
      ReadOnlyCollection<string> _ControllerBaseRouteSegments;
      RouteParameterInfoCollection _RouteParameters;
      string _Name;

      public Type Type { get; private set; }
      public string DefaultActionName { get; private set; }
      public string BaseRoute { get; private set; }

      public string Name {
         get {
            if (_Name == null) {
               string controllerName = Type.Name.Substring(0, Type.Name.Length - 10);
               _Name = settings.RouteFormatter(controllerName, RouteSegmentType.Controller);
               CodeRoutingSettings.CheckCaseFormattingOnly(controllerName, _Name, RouteSegmentType.Controller);
            }
            return _Name;
         }
      }

      public bool IsInRootNamespace {
         get {
            return Type.Namespace == rootNamespace
               || IsInSubNamespace;
         }
      }

      public bool IsInSubNamespace {
         get {
            return Type.Namespace.Length > rootNamespace.Length
               && Type.Namespace.StartsWith(rootNamespace + ".", StringComparison.Ordinal);
         }
      }

      public bool IsRootController {
         get {
            return !String.IsNullOrWhiteSpace(RootController)
               && NamespaceRouteParts.Count == 0
               && NameEquals(Name, RootController);
         }
      }

      public ReadOnlyCollection<string> NamespaceRouteParts {
         get {
            if (_NamespaceRouteParts == null) {
               var namespaceParts = new List<string>();

               if (IsInSubNamespace) {
                  namespaceParts.AddRange(
                     Type.Namespace.Remove(0, rootNamespace.Length + 1).Split('.')
                  );

                  if (namespaceParts.Count > 0 && NameEquals(namespaceParts.Last(), Name))
                     namespaceParts.RemoveAt(namespaceParts.Count - 1);
               }

               _NamespaceRouteParts = new ReadOnlyCollection<string>(namespaceParts);
            }
            return _NamespaceRouteParts;
         }
      }

      public ReadOnlyCollection<string> ControllerBaseRouteSegments {
         get {
            if (_ControllerBaseRouteSegments == null) {
               string[] nsSegments = NamespaceRouteParts.Select(s => settings.RouteFormatter(s, RouteSegmentType.Namespace)).ToArray();

               for (int i = 0; i < nsSegments.Length; i++)
                  CodeRoutingSettings.CheckCaseFormattingOnly(NamespaceRouteParts[i], nsSegments[i], RouteSegmentType.Namespace);

               if (String.IsNullOrEmpty(BaseRoute)) {
                  _ControllerBaseRouteSegments = new ReadOnlyCollection<string>(nsSegments);
               } else {
                  var segments = new List<string>();
                  segments.AddRange(BaseRoute.Split('/'));
                  segments.AddRange(nsSegments);

                  _ControllerBaseRouteSegments = new ReadOnlyCollection<string>(segments);
               }
            }
            return _ControllerBaseRouteSegments;
         }
      }

      public RouteParameterInfoCollection RouteParameters {
         get {
            if (_RouteParameters == null) {

               var types = new List<Type>();

               for (Type t = this.Type; t != null; t = t.BaseType) 
                  types.Add(t);

               types.Reverse();

               var list = new List<RouteParameterInfo>();

               foreach (var type in types) {
                  list.AddRange(
                     from p in type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public)
                     where p.IsDefined(typeof(FromRouteAttribute), inherit: false)
                     let rp = new RouteParameterInfo(p, settings.DefaultConstraints)
                     where !list.Any(item => RouteParameterInfo.NameEquals(item.Name, rp.Name))
                     select rp
                  );
               }

               _RouteParameters = new RouteParameterInfoCollection(list);
            }
            return _RouteParameters;
         }
      }

      public string UrlTemplate {
         get {
            return String.Join("/", ControllerBaseRouteSegments
               .Concat((!IsRootController) ? new[] { "{controller}" } : new string[0])
               .Concat(RouteParameters.Select(p => p.RouteSegment))
            );
         }
      }

      public string ControllerUrl {
         get {
            return String.Join("/", ControllerBaseRouteSegments
               .Concat((!IsRootController) ? new[] { Name } : new string[0])
               .Concat(RouteParameters.Select(p => p.RouteSegment))
            );
         }
      }

      public static IEnumerable<ControllerInfo> GetControllers(Assembly assembly, string rootNamespace, string baseRoute, CodeRoutingSettings settings) {

         return
            from t in assembly.GetTypes()
            where t.IsPublic
              && !t.IsAbstract
              && baseType.IsAssignableFrom(t)
              && t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)
              && !settings.IgnoredControllers.Contains(t)
            let controllerInfo = new ControllerInfo(t, rootNamespace, baseRoute, settings)
            where controllerInfo.IsInRootNamespace
            select controllerInfo;
      }

      public static bool NameEquals(string name1, string name2) {
         return String.Equals(name1, name2, StringComparison.OrdinalIgnoreCase);
      }

      public ControllerInfo(Type type, string rootNamespace, string baseRoute, CodeRoutingSettings settings) {

         this.Type = type;
         this.rootNamespace = rootNamespace;
         this.BaseRoute = baseRoute;
         this.settings = settings;
         this.DefaultActionName = DefaultAction;
      }

      public IEnumerable<ActionInfo> GetActions() {

         bool controllerIsDisposable = typeof(IDisposable).IsAssignableFrom(this.Type);

         var actions =
             from m in this.Type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
             where !m.IsSpecialName
                && baseType.IsAssignableFrom(m.DeclaringType)
                && !Attribute.IsDefined(m, typeof(NonActionAttribute))
                && !(controllerIsDisposable && m.Name == "Dispose" && m.ReturnType == typeof(void) && m.GetParameters().Length == 0)
             select new ActionInfo(m, this, this.settings);

         CheckOverloads(actions);

         return actions;
      }

      static void CheckOverloads(IEnumerable<ActionInfo> actions) {

         var overloadedActions =
            (from a in actions
             where a.RouteParameters.Count > 0
             group a by new { a.Controller, a.Name } into g
             where g.Count() > 1
             select g).ToList();

         var withoutRequiredAttr =
            (from g in overloadedActions
             let distinctParamCount = g.Select(a => a.RouteParameters.Count).Distinct()
             where distinctParamCount.Count() > 1
             let bad = g.Where(a => !a.HasRequireRouteParametersAttribute)
             where bad.Count() > 0
             select bad).ToList();

         if (withoutRequiredAttr.Count > 0) {
            var first = withoutRequiredAttr.First();

            throw new InvalidOperationException(
               String.Format(CultureInfo.InvariantCulture,
                  "The following action methods must be decorated with the {0} for disambiguation: {1}.",
                  typeof(RequireRouteParametersAttribute).FullName,
                  String.Join(", ", first.Select(a => String.Concat(a.Method.DeclaringType.FullName, ".", a.Method.Name, "(", String.Join(", ", a.Method.GetParameters().Select(p => p.ParameterType.Name)), ")")))
               )
            );
         }

         var overloadsComparer = new ActionSignatureComparer();

         var overloadsWithDifferentParameters =
            (from g in overloadedActions
             let ordered = g.OrderByDescending(a => a.RouteParameters.Count).ToArray()
             let first = ordered.First()
             where !ordered.Skip(1).All(a => overloadsComparer.Equals(first, a))
             select g).ToList();

         if (overloadsWithDifferentParameters.Count > 0) {
            var first = overloadsWithDifferentParameters.First();

            throw new InvalidOperationException(
               String.Format(CultureInfo.InvariantCulture,
                  "Overloaded action methods must have parameters that are equal in name, position and constraint ({0}).",
                  String.Concat(first.Key.Controller.Type.FullName, ".", first.First().Method.Name)
               )
            );
         }
      }
   }
}
