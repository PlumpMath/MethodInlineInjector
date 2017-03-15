﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml;

namespace LostPolygon.MethodInlineInjector.Serialization {
    public static class SimpleXmlSerializableExtensions {
        public static void ProcessCollection<T>(
            this SimpleXmlSerializable simpleXmlSerializable,
            ICollection<T> collection,
            Func<T> createItemFunc = null)
            where T : class, ISimpleXmlSerializable {
            if (simpleXmlSerializable.SerializationHelper.IsXmlSerializationReading) {
                while (simpleXmlSerializable.SerializationHelper.XmlSerializationReader.NodeType != XmlNodeType.EndElement) {
                    T value = createItemFunc?.Invoke() ?? Activator.CreateInstance<T>();
                    value.SerializeWithInheritedMode(simpleXmlSerializable);
                    collection.Add(value);
                }
            } else {
                foreach (T value in collection) {
                    value.SerializeWithInheritedMode(simpleXmlSerializable);
                }
            }
        }

        public static void ProcessCollectionAsReadonly<T>(
            this SimpleXmlSerializable simpleXmlSerializable,
            Action<ReadOnlyCollection<T>> collectionSetAction,
            Func<ReadOnlyCollection<T>> collectionGetFunc,
            Func<T> createItemFunc = null)
            where T : class, ISimpleXmlSerializable {
            if (simpleXmlSerializable.SerializationHelper.IsXmlSerializationReading) {
                List<T> list = new List<T>();
                ProcessCollection(simpleXmlSerializable, list, createItemFunc);
                collectionSetAction(list.AsReadOnly());
            } else {
                ProcessCollection(simpleXmlSerializable, collectionGetFunc(), createItemFunc);
            }
        }
    }
}