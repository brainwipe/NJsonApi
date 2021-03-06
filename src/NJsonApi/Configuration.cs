﻿using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NJsonApi.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Microsoft.Net.Http.Headers;
using NJsonApi.Utils;
using NJsonApi.Exceptions;
using NJsonApi.Web;

namespace NJsonApi
{
    public class Configuration : IConfiguration
    {
        private readonly Dictionary<string, IResourceMapping> resourcesMappingsByResourceType = new Dictionary<string, IResourceMapping>();
        private readonly Dictionary<Type, IResourceMapping> resourcesMappingsByType = new Dictionary<Type, IResourceMapping>();

        public Configuration()
        {
            DefaultJsonApiMediaType = new MediaTypeHeaderValue("application/vnd.api+json");
        }

        public MediaTypeHeaderValue DefaultJsonApiMediaType { get; private set; }

        public void AddMapping(IResourceMapping resourceMapping)
        {
            resourcesMappingsByResourceType[resourceMapping.ResourceType] = resourceMapping;
            resourcesMappingsByType[resourceMapping.ResourceRepresentationType] = resourceMapping;
        }

        public bool IsMappingRegistered(Type type)
        {
            if (typeof(IEnumerable).IsAssignableFrom(type) && type.GetTypeInfo().IsGenericType)
            {
                return resourcesMappingsByType.ContainsKey(type.GetGenericArguments()[0]);
            }

            return resourcesMappingsByType.ContainsKey(type);
        }

        public IResourceMapping GetMapping(Type type)
        {
            IResourceMapping mapping;
            resourcesMappingsByType.TryGetValue(type, out mapping);
            return mapping;
        }

        public IResourceMapping GetMapping(object objectGraph)
        {
            return GetMapping(Reflection.GetObjectType(objectGraph));
        }

        public void Apply(IServiceCollection services)
        {
            services.AddMvc(
                options =>
                    {
                        options.Conventions.Add(new ApiExplorerVisibilityEnabledConvention());
                        options.Filters.Add(typeof(JsonApiActionFilter));
                        options.Filters.Add(typeof(JsonApiExceptionFilter));
                        options.OutputFormatters.Insert(0, new JsonApiOutputFormatter(this));
                    });

            services.AddSingleton<ILinkBuilder, LinkBuilder>();
            services.AddInstance<JsonSerializer>(GetJsonSerializer());
            services.AddSingleton<IJsonApiTransformer, JsonApiTransformer>();
            services.AddInstance<IConfiguration>(this);
            services.AddSingleton<TransformationHelper>();
        }

        public bool ValidateIncludedRelationshipPaths(string[] includedPaths, object objectGraph)
        {
            var mapping = GetMapping(objectGraph);
            if (mapping == null)
            {
                throw new MissingMappingException(Reflection.GetObjectType(objectGraph));
            }
            return mapping.ValidateIncludedRelationshipPaths(includedPaths);
        }

        private static JsonSerializer GetJsonSerializer()
        {
            var serializerSettings = new JsonSerializerSettings();
            serializerSettings.Converters.Add(new IsoDateTimeConverter());
            serializerSettings.Converters.Add(new StringEnumConverter() { CamelCaseText = true});
#if DEBUG
            serializerSettings.Formatting = Formatting.Indented;
#endif
            var jsonSerializer = JsonSerializer.Create(serializerSettings);
            return jsonSerializer;
        }

        public IEnumerable<IResourceMapping> All()
        {
            return resourcesMappingsByType.Values;
        }
    }
}
