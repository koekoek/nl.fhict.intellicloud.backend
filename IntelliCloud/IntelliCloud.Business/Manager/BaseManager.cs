﻿using nl.fhict.IntelliCloud.Business.Authorization;
using nl.fhict.IntelliCloud.Common.DataTransfer;
using nl.fhict.IntelliCloud.Data.Context;
using System.ServiceModel.Web;

namespace nl.fhict.IntelliCloud.Business.Manager
{
    /// <summary>
    /// Other managers will inherit this BaseManager.
    /// This manager has the Validation and ConvertEntities class. Also the IntelliCloudContext for tests.
    /// </summary>
    public abstract class BaseManager
    {
        protected IValidation Validation {get; set;}
        protected ConvertEntities ConvertEntities {get; set;}

        /// <summary>
        /// This constructor will construct the BaseManager and instantiate it's properties.
        /// The IValidation property is set to the given value.
        /// </summary>
        /// <param name="validation">IValidation to be set.</param>
        protected BaseManager(IValidation validation)
        {
            Validation = validation;
            ConvertEntities = new ConvertEntities();
        }

        /// <summary>
        /// This constructor will construct the BaseManager and instantiate it's properties.
        /// </summary>
        protected BaseManager()
        {
            Validation = new Validation();
            ConvertEntities = new ConvertEntities();
        }
    }
}
