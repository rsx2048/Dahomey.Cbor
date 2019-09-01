﻿using Dahomey.Cbor.Attributes;
using Dahomey.Cbor.Serialization.Conventions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dahomey.Cbor.Serialization.Converters.Mappings
{
    public class DefaultObjectMappingConvention : IObjectMappingConvention
    {
        public void Apply<T>(SerializationRegistry registry, ObjectMapping<T> objectMapping) where T : class
        {
            Type type = objectMapping.ObjectType;
            List<MemberMapping> memberMappings = new List<MemberMapping>();

            ReadOnlyMemory<byte> typeName = Encoding.ASCII.GetBytes($"{type.FullName}, {type.Assembly.GetName().Name}");
            CborDiscriminatorAttribute discriminatorAttribute = type.GetCustomAttribute<CborDiscriminatorAttribute>();

            ReadOnlyMemory<byte> discriminator = Encoding.UTF8.GetBytes(discriminatorAttribute != null ?
                discriminatorAttribute.Discriminator :
                $"{type.FullName}, {type.Assembly.GetName().Name}");

            registry.DefaultDiscriminatorConvention.RegisterType(type, discriminator);

            Type namingConventionType = type.GetCustomAttribute<CborNamingConventionAttribute>()?.NamingConventionType;
            if (namingConventionType != null)
            {
                objectMapping.SetNamingConvention((INamingConvention)Activator.CreateInstance(namingConventionType));
            }

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (PropertyInfo propertyInfo in properties)
            {
                if (propertyInfo.IsDefined(typeof(CborIgnoreAttribute)))
                {
                    continue;
                }

                if ((propertyInfo.GetMethod.IsPrivate || propertyInfo.GetMethod.IsStatic) && !propertyInfo.IsDefined(typeof(CborPropertyAttribute)))
                {
                    continue;
                }

                MemberMapping memberMapping = new MemberMapping(registry.ConverterRegistry, objectMapping, propertyInfo, propertyInfo.PropertyType);
                ProcessDefaultValue(propertyInfo, memberMapping);
                memberMappings.Add(memberMapping);
            }

            foreach (FieldInfo fieldInfo in fields)
            {
                if (fieldInfo.IsDefined(typeof(CborIgnoreAttribute)))
                {
                    continue;
                }

                if ((fieldInfo.IsPrivate || fieldInfo.IsStatic) && !fieldInfo.IsDefined(typeof(CborPropertyAttribute)))
                {
                    continue;
                }

                Type fieldType = fieldInfo.FieldType;

                MemberMapping memberMapping = new MemberMapping(registry.ConverterRegistry, objectMapping, fieldInfo, fieldInfo.FieldType);
                ProcessDefaultValue(fieldInfo, memberMapping);

                memberMappings.Add(memberMapping);
            }

            objectMapping.SetMemberMappings(memberMappings);

            ConstructorInfo constructorInfo = type.GetConstructors()
                .FirstOrDefault(c => c.IsDefined(typeof(CborConstructorAttribute)));

            if (constructorInfo != null)
            {
                CborConstructorAttribute constructorAttribute = constructorInfo.GetCustomAttribute<CborConstructorAttribute>();
                CreatorMapping creatorMapping = objectMapping.MapCreator(constructorInfo);
                if (constructorAttribute.MemberNames != null)
                {
                    creatorMapping.SetMemberNames(constructorAttribute.MemberNames);
                }
            }
        }

        private void ProcessDefaultValue(MemberInfo memberInfo, MemberMapping memberMapping)
        {
            DefaultValueAttribute defaultValueAttribute = memberInfo.GetCustomAttribute<DefaultValueAttribute>();
            if (defaultValueAttribute != null)
            {
                memberMapping.SetDefaultValue(defaultValueAttribute.Value);
            }

            if (memberInfo.IsDefined(typeof(CborIgnoreIfDefaultAttribute)))
            {
                memberMapping.SetIngoreIfDefault(true);
            }
        }
    }
}