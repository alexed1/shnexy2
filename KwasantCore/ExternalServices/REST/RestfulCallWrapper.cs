﻿using System;
using KwasantCore.Managers.APIManager.Transmitters.Restful;
using Utilities;

namespace KwasantCore.ExternalServices.REST
{
    public class RestfulCallWrapper : IRestfullCall
    {
        private RestfulCall _client;
        private ServiceManager<RestfulCallWrapper> _serviceManager;

        public void Initialize(string baseURL, string resource, Method method)
        {
            _serviceManager = new ServiceManager<RestfulCallWrapper>("REST Service");
            _client = new RestfulCall(baseURL, resource, method);
        }

        public void AddBody(string body, string contentType)
        {
            _client.AddBody(body, contentType);
        }

        public IRestfulResponse Execute()
        {
            _serviceManager.LogAttempt("Sending REST call...");
            var result = new RestfulResponseWrapper(_client.Execute());
            _serviceManager.LogSucessful("REST call sent.");
            return result;
        }
    }
}
