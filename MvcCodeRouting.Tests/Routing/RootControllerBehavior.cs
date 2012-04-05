﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MvcCodeRouting.Tests.Routing {
   
   [TestClass]
   public class RootControllerBehavior {

      RouteCollection routes;
      UrlHelper Url;

      [TestInitialize]
      public void Init() {

         this.routes = new RouteCollection();
         this.Url = TestUtil.CreateUrlHelper(routes);
      }

      [TestMethod]
      public void DontIncludeControllerToken() {

         var controller = typeof(RootController1Controller);

         routes.Clear();
         routes.MapCodeRoutes(controller, new CodeRoutingSettings { RootOnly = true });

         Assert.IsTrue(!routes.At(0).Url.Contains("{controller}"));
      }
   }

   public class RootController1Controller : Controller {
      public void Index() { }
   }
}