﻿using System;
using System.Linq;
using Microsoft.AspNet.Http.Extensions;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.AspNet.Mvc;
using NJsonApi.Web.BadActionResultTransformers;
using Microsoft.AspNet.Http;
using NJsonApi.Serialization;
using Microsoft.AspNet.Mvc.ApiExplorer;
using System.IO;
using Newtonsoft.Json;
using Microsoft.AspNet.Mvc.ModelBinding;

namespace NJsonApi.Web
{
    internal class JsonApiActionFilter : ActionFilterAttribute
    {
        public bool AllowMultiple { get { return false; } }
        private readonly IJsonApiTransformer jsonApiTransformer;
        private readonly IConfiguration configuration;
        private readonly JsonSerializer serializer;

        public JsonApiActionFilter(
            IJsonApiTransformer jsonApiTransformer, 
            IConfiguration configuration,
            JsonSerializer serializer)
        {
            this.jsonApiTransformer = jsonApiTransformer;
            this.configuration = configuration;
            this.serializer = serializer;
        }
    
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.Request.ContentType != configuration.DefaultJsonApiMediaType.MediaType)
            {
                context.Result = new UnsupportedMediaTypeResult();
            }

            if (!ValidateAcceptHeader(context.HttpContext.Request.Headers))
            { 
                context.Result = new HttpStatusCodeResult(406);
            }

            using (var reader = new StreamReader(context.HttpContext.Request.Body))
            {
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var updateDocument = serializer.Deserialize(jsonReader, typeof(UpdateDocument)) as UpdateDocument;

                    if (updateDocument != null)
                    {
                        var actionDescriptorForBody = context.ActionDescriptor
                            .Parameters
                            .Single(x => x.BindingInfo.BindingSource == BindingSource.Body);

                        var typeInsideDeltaGeneric = actionDescriptorForBody
                            .ParameterType
                            .GenericTypeArguments
                            .Single();
                        var jsonApiContext = new Context(new Uri(context.HttpContext.Request.Host.Value, UriKind.Absolute));
                        var transformed = jsonApiTransformer.TransformBack(updateDocument, typeInsideDeltaGeneric, jsonApiContext);
                        context.ActionArguments.Add(actionDescriptorForBody.Name, transformed);
                        context.ModelState.Clear();
                    }
                }
            }
        }

        public override void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Result == null || context.Result is NoContentResult)
            {
                return;
            }

            if (BadActionResultTransformer.IsBadAction(context.Result))
            {
                var transformed = BadActionResultTransformer.Transform(context.Result);

                context.Result = new ObjectResult(transformed)
                {
                    StatusCode = transformed.Errors.First().Status
                };
                return;
            }

            var responseResult = (ObjectResult)context.Result;
            var relationshipPaths = FindRelationshipPathsToInclude(context.HttpContext.Request);

            if (!configuration.ValidateIncludedRelationshipPaths(relationshipPaths, responseResult.Value))
            {
                context.Result = new HttpStatusCodeResult(400);
                return;
            }

            var jsonApiContext = new Context(
                new Uri(context.HttpContext.Request.GetDisplayUrl()),
                relationshipPaths);
            responseResult.Value = jsonApiTransformer.Transform(responseResult.Value, jsonApiContext);
        }

        private string[] FindRelationshipPathsToInclude(HttpRequest request)
        {
            var result = request.Query["include"].FirstOrDefault();

            return string.IsNullOrEmpty(result) ? new string[0] : result.Split(',');
        }

        private bool ValidateAcceptHeader(IHeaderDictionary headers)
        {
            var acceptsHeaders = headers["Accept"].FirstOrDefault();

            if (string.IsNullOrEmpty(acceptsHeaders))
            {
                return true;
            }

            return acceptsHeaders
                .Split(',')
                .Select(x => x.Trim())
                .Any(x => 
                    x == "*/*" || 
                    x == configuration.DefaultJsonApiMediaType.MediaType);
        }       
    }
}