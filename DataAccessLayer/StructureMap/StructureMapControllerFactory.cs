﻿using System;
using System.Web.Mvc;
using System.Web.Routing;
using StructureMap;

namespace Shnexy.DataAccessLayer.StructureMap
{
    public class StructureMapControllerFactory : DefaultControllerFactory
    {
        #region Method

        protected override IController GetControllerInstance(RequestContext requestContext, Type controllerType)
        {
            return ObjectFactory.GetInstance(controllerType) as IController;
        }

        #endregion
    }
}