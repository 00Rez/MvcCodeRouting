﻿using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace MvcCodeRouting.Tests.ModelBinding {
   
   //[TestClass]
   public class BinderPrecedenceFacts {

      readonly RouteCollection routes;

      public BinderPrecedenceFacts() {
         routes = TestUtil.GetRouteCollection();
      }

      [TestMethod]
      public void TypeVsParameter_ParameterWins() {

         var controller = typeof(BinderPrecedence.TypeVsParameter.BinderPrecedenceController);

         routes.Clear();
         routes.MapCodeRoutes(controller);

         var httpContextMock = new Mock<HttpContextBase>();
         httpContextMock.Setup(c => c.Request.AppRelativeCurrentExecutionFilePath).Returns("~/");

         var httpResponseMock = new Mock<HttpResponseBase>();
         httpContextMock.Setup(c => c.Response).Returns(httpResponseMock.Object);

         var routeData = routes.GetRouteData(httpContextMock.Object);

         var controllerInstance = (Controller)Activator.CreateInstance(controller);
         controllerInstance.ValidateRequest = false;

         var requestContext = new RequestContext(httpContextMock.Object, routeData);
         var controllerContext = new ControllerContext(requestContext, controllerInstance);

         controllerInstance.ValueProvider = new ValueProviderCollection(new IValueProvider[] { new RouteDataValueProvider(controllerContext) });

         ((IController)controllerInstance).Execute(requestContext); 

         httpResponseMock.Verify(c => c.Write(It.Is<string>(s => s == BinderPrecedence.BinderPrecedence.Parameter.ToString())), Times.AtLeastOnce());
      }

      [TestMethod]
      public void TypeVsGlobal_GlobalWins() {

         var controller = typeof(BinderPrecedence.TypeVsGlobal.BinderPrecedenceController);

         routes.Clear();
         routes.MapCodeRoutes(controller);

         var httpContextMock = new Mock<HttpContextBase>();
         httpContextMock.Setup(c => c.Request.AppRelativeCurrentExecutionFilePath).Returns("~/");

         var httpResponseMock = new Mock<HttpResponseBase>();
         httpContextMock.Setup(c => c.Response).Returns(httpResponseMock.Object);

         var routeData = routes.GetRouteData(httpContextMock.Object);

         var controllerInstance = (Controller)Activator.CreateInstance(controller);
         controllerInstance.ValidateRequest = false;

         var requestContext = new RequestContext(httpContextMock.Object, routeData);
         var controllerContext = new ControllerContext(requestContext, controllerInstance);

         controllerInstance.ValueProvider = new ValueProviderCollection(new IValueProvider[] { new RouteDataValueProvider(controllerContext) });

         ((IController)controllerInstance).Execute(requestContext);

         httpResponseMock.Verify(c => c.Write(It.Is<string>(s => s == BinderPrecedence.BinderPrecedence.Global.ToString())), Times.AtLeastOnce());
      }

      [TestMethod]
      public void ParameterVsGlobal_ParameterWins() {

         var controller = typeof(BinderPrecedence.ParameterVsGlobal.BinderPrecedenceController);

         routes.Clear();
         routes.MapCodeRoutes(controller);

         var httpContextMock = new Mock<HttpContextBase>();
         httpContextMock.Setup(c => c.Request.AppRelativeCurrentExecutionFilePath).Returns("~/");

         var httpResponseMock = new Mock<HttpResponseBase>();
         httpContextMock.Setup(c => c.Response).Returns(httpResponseMock.Object);

         var routeData = routes.GetRouteData(httpContextMock.Object);

         var controllerInstance = (Controller)Activator.CreateInstance(controller);
         controllerInstance.ValidateRequest = false;

         var requestContext = new RequestContext(httpContextMock.Object, routeData);
         var controllerContext = new ControllerContext(requestContext, controllerInstance);

         controllerInstance.ValueProvider = new ValueProviderCollection(new IValueProvider[] { new RouteDataValueProvider(controllerContext) });

         ((IController)controllerInstance).Execute(requestContext);

         httpResponseMock.Verify(c => c.Write(It.Is<string>(s => s == BinderPrecedence.BinderPrecedence.Parameter.ToString())), Times.AtLeastOnce());
      }
   }
}

namespace MvcCodeRouting.Tests.ModelBinding.BinderPrecedence {

   public enum BinderPrecedence {
      None = 0,
      Parameter,
      Type,
      Global
   } 
}

namespace MvcCodeRouting.Tests.ModelBinding.BinderPrecedence.TypeVsParameter {

   public class BinderPrecedenceController : Controller {

      public string Index([ModelBinder(typeof(BinderPrecedenceParameterBinder))]BinderPrecedenceModel model) {
         return model.Precedence.ToString();
      }
   }

   [ModelBinder(typeof(BinderPrecedenceTypeBinder))]
   public class BinderPrecedenceModel {
      public BinderPrecedence Precedence { get; set; }
   }

   class BinderPrecedenceParameterBinder : IModelBinder {

      public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext) {
         return new BinderPrecedenceModel { 
            Precedence = BinderPrecedence.Parameter
         };
      }
   }

   class BinderPrecedenceTypeBinder : IModelBinder {

      public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext) {
         return new BinderPrecedenceModel {
            Precedence = BinderPrecedence.Type
         };
      }
   }
}

namespace MvcCodeRouting.Tests.ModelBinding.BinderPrecedence.TypeVsGlobal {

   public class BinderPrecedenceController : Controller {

      static BinderPrecedenceController() {
         ModelBinders.Binders[typeof(BinderPrecedenceModel)] = new BinderPrecedenceGlobalBinder();
      }

      public string Index(BinderPrecedenceModel model) {
         return model.Precedence.ToString();
      }
   }

   [ModelBinder(typeof(BinderPrecedenceTypeBinder))]
   public class BinderPrecedenceModel {
      public BinderPrecedence Precedence { get; set; }
   }

   class BinderPrecedenceTypeBinder : IModelBinder {

      public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext) {
         return new BinderPrecedenceModel {
            Precedence = BinderPrecedence.Type
         };
      }
   }

   class BinderPrecedenceGlobalBinder : IModelBinder {

      public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext) {
         return new BinderPrecedenceModel {
            Precedence = BinderPrecedence.Global
         };
      }
   }
}

namespace MvcCodeRouting.Tests.ModelBinding.BinderPrecedence.ParameterVsGlobal {

   public class BinderPrecedenceController : Controller {

      static BinderPrecedenceController() {
         ModelBinders.Binders[typeof(BinderPrecedenceModel)] = new BinderPrecedenceGlobalBinder();
      }

      public string Index([ModelBinder(typeof(BinderPrecedenceParameterBinder))]BinderPrecedenceModel model) {
         return model.Precedence.ToString();
      }
   }

   public class BinderPrecedenceModel {
      public BinderPrecedence Precedence { get; set; }
   }

   class BinderPrecedenceParameterBinder : IModelBinder {

      public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext) {
         return new BinderPrecedenceModel {
            Precedence = BinderPrecedence.Parameter
         };
      }
   }

   class BinderPrecedenceGlobalBinder : IModelBinder {

      public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext) {
         return new BinderPrecedenceModel {
            Precedence = BinderPrecedence.Global
         };
      }
   }
}