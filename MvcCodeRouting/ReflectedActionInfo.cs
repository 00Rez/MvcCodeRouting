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
using System.Linq;
using System.Text;
using System.Reflection;
using System.Web.Mvc;

namespace MvcCodeRouting {

   class ReflectedActionInfo : ActionInfo {

      readonly MethodInfo method;
      string _Name;

      public override string Name {
         get {
            if (_Name == null) {
               ActionNameAttribute nameAttr = GetCustomAttributes(typeof(ActionNameAttribute), inherit: true)
                  .Cast<ActionNameAttribute>()
                  .SingleOrDefault();

               _Name = (nameAttr != null) ? 
                  nameAttr.Name : method.Name;
            }
            return _Name;
         }
      }

      public override string MethodName {
         get { return method.Name; }
      }

      public override Type DeclaringType {
         get { return method.DeclaringType; }
      }

      public ReflectedActionInfo(MethodInfo method, ControllerInfo controller)
         : base(controller) {

         this.method = method;
      }

      protected override ActionParameterInfo[] GetParameters() {
         return this.method.GetParameters().Select(p => new ReflectedActionParameterInfo(p, this)).ToArray();
      }

      public override object[] GetCustomAttributes(bool inherit) {
         return this.method.GetCustomAttributes(inherit);
      }

      public override object[] GetCustomAttributes(Type attributeType, bool inherit) {
         return this.method.GetCustomAttributes(attributeType, inherit);
      }

      public override bool IsDefined(Type attributeType, bool inherit) {
         return this.method.IsDefined(attributeType, inherit);
      }
   }
}
