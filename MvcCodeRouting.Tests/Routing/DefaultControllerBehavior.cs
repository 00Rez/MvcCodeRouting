﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace MvcCodeRouting.Tests.Routing {
   
   [TestClass]
   public class DefaultControllerBehavior {

      RouteCollection routes;
      UrlHelper Url;

      [TestInitialize]
      public void Init() {

         this.routes = new RouteCollection();
         this.Url = TestUtil.CreateUrlHelper(routes);
      }

      [TestMethod]
      public void SharesParentNamespaceUrlSpace() {

         var controller = typeof(DefaultController.DefaultController1Controller);

         routes.Clear();
         routes.MapCodeRoutes(controller, new CodeRoutingSettings {
            IgnoredControllers = { typeof(DefaultController.DefaultController3.DefaultController3Controller) }
         });

         var httpContextMock = new Mock<HttpContextBase>();
         httpContextMock.Setup(c => c.Request.AppRelativeCurrentExecutionFilePath).Returns("~/DefaultController2");

         var routeData = routes.GetRouteData(httpContextMock.Object);

         Assert.AreEqual(routeData.GetRequiredString("controller"), "DefaultController2");
         Assert.AreEqual(((string[])routeData.DataTokens["Namespaces"])[0], controller.Namespace);

         httpContextMock.Setup(c => c.Request.AppRelativeCurrentExecutionFilePath).Returns("~/DefaultController2/Foo");

         routeData = routes.GetRouteData(httpContextMock.Object);

         Assert.AreEqual(routeData.GetRequiredString("controller"), "DefaultController2");
         Assert.AreEqual(((string[])routeData.DataTokens["Namespaces"])[0], typeof(DefaultController.DefaultController2.DefaultController2Controller).Namespace);
      }

      [TestMethod]
      public void IsSiblingWithParentNamespaceControllers() {

         var controller = typeof(DefaultController.DefaultController1Controller);

         routes.Clear();
         routes.MapCodeRoutes(controller, new CodeRoutingSettings {
            IgnoredControllers = { typeof(DefaultController.DefaultController3.DefaultController3Controller) }
         });

         Assert.IsNotNull(Url.Action("", "DefaultController2"));
         Assert.IsNotNull(Url.Action("Foo", "DefaultController2"));
      }

      [TestMethod]
      [ExpectedException(typeof(InvalidOperationException))]
      public void ThrowsExceptionForAmbiguousUrls() {

         var controller = typeof(DefaultController.DefaultController1Controller);

         routes.Clear();
         routes.MapCodeRoutes(controller);
      }
   }
}

namespace MvcCodeRouting.Tests.Routing.DefaultController {

   public class DefaultController1Controller : Controller {
      public void Index() { }
   }

   public class DefaultController2Controller : Controller {
      public void Index() { }
   }

   public class DefaultController3Controller : Controller {
      public void Index() { }
   }
}

namespace MvcCodeRouting.Tests.Routing.DefaultController.DefaultController2 {

   public class DefaultController2Controller : Controller {
      public void Foo() { }
   }
}

namespace MvcCodeRouting.Tests.Routing.DefaultController.DefaultController3 {

   public class DefaultController3Controller : Controller {
      public void Index() { }
   }
}