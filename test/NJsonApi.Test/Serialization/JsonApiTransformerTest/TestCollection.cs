﻿using System;
using System.Collections.Generic;
using System.Linq;
using NJsonApi.Infrastructure;
using NJsonApi.Serialization;
using NJsonApi.Serialization.Documents;
using NJsonApi.Serialization.Representations;
using NJsonApi.Serialization.Representations.Resources;
using Xunit;
using NJsonApi.Test.Builders;
using NJsonApi.Test.TestControllers;

namespace NJsonApi.Test.Serialization.JsonApiTransformerTest
{
    public class TestCollection
    {
        readonly List<string> reservedKeys = new List<string> { "id", "type", "href", "links" };
           
        [Fact]
        public void Creates_CompondDocument_for_collection_not_nested_class_and_propertly_map_resourceName()
        {
            // Arrange
            var context = CreateContext();            
            IEnumerable<SampleClass> objectsToTransform = CreateObjectToTransform();
            var transformer = new JsonApiTransformerBuilder()
                .With(CreateConfiguration())
                .Build();

            // Act
            CompoundDocument result = transformer.Transform(objectsToTransform, context);

            // Assert
            Assert.NotNull(result.Data);
            var transformedObject = result.Data as ResourceCollection;
            Assert.NotNull(transformedObject);
        }

        [Fact]
        public void Creates_CompondDocument_for_collection_not_nested_single_class()
        {
            // Arrange
            var configuration = CreateContext();
            IEnumerable<SampleClass> objectsToTransform = CreateObjectToTransform().Take(1);
            var transformer = new JsonApiTransformerBuilder()
                .With(CreateConfiguration())
                .Build();

            // Act
            CompoundDocument result = transformer.Transform(objectsToTransform, configuration);

            // Assert
            Assert.NotNull(result.Data);
            var transformedObject = result.Data as ResourceCollection;
            Assert.NotNull(transformedObject);
        }


        [Fact]
        public void Creates_CompondDocument_for_collection_not_nested_class_and_propertly_map_id()
        {
            // Arrange
            var configuration = CreateContext();
            IEnumerable<SampleClass> objectsToTransform = CreateObjectToTransform();
            var transformer = new JsonApiTransformerBuilder()
                .With(CreateConfiguration())
                .Build();

            // Act
            CompoundDocument result = transformer.Transform(objectsToTransform, configuration);

            // Assert
            var transformedObject = result.Data as ResourceCollection;
            Assert.Equal(transformedObject[0].Id, objectsToTransform.First().Id.ToString());
            Assert.Equal(transformedObject[1].Id, objectsToTransform.Last().Id.ToString());
        }

        [Fact]
        public void Creates_CompondDocument_for_collection_not_nested_class_and_propertly_map_properties()
        {
            // Arrange
            var context = CreateContext();
            IEnumerable<SampleClass> objectsToTransform = CreateObjectToTransform();
            var transformer = new JsonApiTransformerBuilder()
                .With(CreateConfiguration())
                .Build();

            // Act
            CompoundDocument result = transformer.Transform(objectsToTransform, context);

            // Assert
            var transformedObject = result.Data as ResourceCollection;

            Action<SingleResource, SampleClass> assertSame = (actual, expected) =>
            {
                Assert.Equal(actual.Attributes["someValue"], expected.SomeValue);
                Assert.Equal(actual.Attributes["date"], expected.DateTime);
                Assert.Equal(actual.Attributes.Count(), 2);
            };

            assertSame(transformedObject[0], objectsToTransform.First());
            assertSame(transformedObject[1], objectsToTransform.Last());
        }

        [Fact]
        public void Creates_CompondDocument_for_collection_not_nested_class_and_propertly_map_type()
        {
            // Arrange
            var configuration = CreateContext();
            IEnumerable<SampleClass> objectsToTransform = CreateObjectToTransform();
            var transformer = new JsonApiTransformerBuilder()
                .With(CreateConfiguration())
                .Build();

            // Act
            CompoundDocument result = transformer.Transform(objectsToTransform, configuration);

            // Assert
            var transformedObject = result.Data as ResourceCollection;
            Assert.Equal(transformedObject[0].Type, "sampleClasses");
            Assert.Equal(transformedObject[1].Type, "sampleClasses");
        }

        [Fact]
        public void Creates_CompondDocument_for_collectione_of_metadatawrapper_throws_notSupportedException()
        {
            // Arrange
            var configuration = CreateContext();
            var objectsToTransform = new List<MetaDataWrapper<SampleClass>>
            {
                new MetaDataWrapper<SampleClass>(new SampleClass())
            };
            var transformer = new JsonApiTransformerBuilder().Build();

            // Act => Assert
            Assert.Throws<NotSupportedException>(() => transformer.Transform(objectsToTransform, configuration));
        }

        private static IEnumerable<SampleClass> CreateObjectToTransform()
        {
            var objectToTransformOne = new SampleClass
            {
                Id = 1,
                SomeValue = "Somevalue text test string",
                DateTime = DateTime.UtcNow,
                NotMappedValue = "Should be not mapped"
            };

            var objectToTransformTwo = new SampleClass
            {
                Id = 2,
                SomeValue = "Somevalue text test string",
                DateTime = DateTime.UtcNow.AddDays(1),
                NotMappedValue = "Should be not mapped"
            };

            return new List<SampleClass>()
            {
                objectToTransformOne,
                objectToTransformTwo
            };
        }

        private Context CreateContext()
        {
            
            var requestUri = new Uri("http://fakeUri:1234/fakecontroller");

            return new Context(requestUri);
        }

        private IConfiguration CreateConfiguration()
        {
            var mapping = new ResourceMapping<SampleClass, DummyController>(c => c.Id, "http://sampleClass/{id}");
            mapping.ResourceType = "sampleClasses";
            mapping.AddPropertyGetter("someValue", c => c.SomeValue);
            mapping.AddPropertyGetter("date", c => c.DateTime);
            var config = new NJsonApi.Configuration();
            config.AddMapping(mapping);
            return config;
        }

        class SampleClass
        {
            public int Id { get; set; }
            public string SomeValue { get; set; }
            public DateTime DateTime { get; set; }
            public string NotMappedValue { get; set; }
        }
    }
}