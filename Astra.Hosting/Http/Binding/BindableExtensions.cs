using Astra.Hosting.Http.Binding.Attributes;
using Astra.Hosting.Http.Interfaces;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Astra.Hosting.Http.Binding
{
    public static class BindableExtensions
    {
        private static readonly ILogger _logger = ModuleInitialization.InitializeLogger("BindableExtensions");

        private static object ConvertValueStringToType(Type type, string value)
        {
            try
            {
                if (type.IsEnum)
                {
                    var intValue = int.Parse(value);
                    
                    if (!Enum.IsDefined(type, intValue))
                        throw new ArgumentException($"The value '{value}' is not defined in enum {type.Name}.");
                    return Enum.Parse(type, value);
                }
        
                var typeConverter = TypeDescriptor.GetConverter(type);
                if (typeConverter == null)
                {
                    _logger.Error("The TypeConverter yielded was null for type {Type}. Cannot convert.", type);
                    return null!;
                }

                return typeConverter.ConvertFromString(value);
            }
            catch (ArgumentException ex)
            {
                _logger.Error("Failed to convert '{Value}' to {Type}: {Message}", value, type, ex.Message);
                return null!;
            }
            catch (Exception ex)
            {
                _logger.Error("An unexpected error occurred in ConvertValueStringToType: {Message}", ex.Message);
                return null!;
            }
        }


        //
        // if method parameters contain FromFormAttribute, FromBodyAttribute, FromHeaderAttribute, or FromQueryAttribute
        // use the attr.Name property to get the form / json body / header / query value from httpContext and set the value 
        // using ConvertValueStringToType
        //
        // if the method parameter is a class, and has the FromFormAttribute, make an instance of the object
        // and scan through the reflection for any properties that have the FromFormAttribute, and set the value
        // using ConvertValueStringToType
        // 
        public static async Task<object[]> BindParameters(MethodInfo methodInfo, IHttpContext context)
        {
            var parameters = methodInfo.GetParameters();
            var args = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var fromForm = param.GetCustomAttribute<FromFormAttribute>();
                var fromHeader = param.GetCustomAttribute<FromHeaderAttribute>();
                var fromQuery = param.GetCustomAttribute<FromQueryAttribute>();
                var fromBody = param.GetCustomAttribute<FromBodyAttribute>();

                if (fromForm != null) args[i] = await BindFromForm(param, fromForm, context);
                else if (fromHeader != null) args[i] = BindFromHeader(param, fromHeader, context);
                else if (fromQuery != null) args[i] = BindFromQuery(param, fromQuery, context);
                else if (fromBody != null) args[i] = BindFromBody(param, fromBody, context);
                else if (param.ParameterType == typeof(IHttpContext))
                {
                    args[i] = context;
                }
                else if (param.ParameterType == typeof(IHttpRequest))
                {
                    args[i] = context.Request;
                }
                else if (param.ParameterType == typeof(IHttpResponse))
                {
                    args[i] = context.Response;
                }
            }
            return args;
        }

        private static object BindFromBody(ParameterInfo param, FromBodyAttribute attr, IHttpContext context)
        {
            if (context.Request.JsonBody == null)
            {
                _logger.Error("JSON body is null when trying to bind FromBody attribute");
                return null!;
            }

            if (param.ParameterType.IsClass && param.ParameterType != typeof(string))
                return BindComplexTypeFromBody(param.ParameterType, context.Request.JsonBody);
            else
            {
                if (context.Request.JsonBody.TryGetValue(attr.Name ?? param.Name!, out var value))
                    return ConvertValueStringToType(param.ParameterType, value.ToString());
                return null!;
            }
        }

        private static object BindComplexTypeFromBody(Type type, Dictionary<string, object> jsonBody)
        {
            var instance = Activator.CreateInstance(type);
            var properties = type.GetProperties();

            foreach (var prop in properties)
            {
                var dataMemberAttr = prop.GetCustomAttribute<DataMemberAttribute?>();
                if (jsonBody.TryGetValue(dataMemberAttr?.Name ?? prop.Name, out var value))
                {
                    var convertedValue = ConvertValueStringToType(prop.PropertyType, value.ToString());
                    prop.SetValue(instance, convertedValue);
                }
            }

            return instance!;
        }

        private static async Task<object> BindFromForm(ParameterInfo param, FromFormAttribute attr, IHttpContext context)
        {
            if (param.ParameterType.IsClass && param.ParameterType != typeof(string))
            {
                return await BindComplexTypeFromForm(param.ParameterType, context);
            }
            else
            {
                if (context.Request.FormBody.TryGetValue(attr.Name ?? param.Name!, out var formValue))
                    return ConvertValueStringToType(param.ParameterType, formValue);
                return null!;
            }
        }

        private static object BindFromHeader(ParameterInfo param, FromHeaderAttribute attr, IHttpContext context)
        {
            var headerValue = context.Request.GetHeaderValue(attr.Name ?? param.Name!);
            return ConvertValueStringToType(param.ParameterType, headerValue);
        }

        private static object BindFromQuery(ParameterInfo param, FromQueryAttribute attr, IHttpContext context)
        {
            var queryValue = context.Request.GetQueryParameter(attr.Name ?? param.Name!);
            return ConvertValueStringToType(param.ParameterType, queryValue);
        }

        private static async Task<object> BindComplexTypeFromForm(Type type, IHttpContext context)
        {
            var instance = Activator.CreateInstance(type);
            var properties = type.GetProperties().Where(p => p.GetCustomAttribute<DataMemberAttribute>() != null);

            foreach (var prop in properties)
            {
                var attr = prop.GetCustomAttribute<DataMemberAttribute>();
                if (context.Request.FormBody.TryGetValue(attr.Name ?? prop.Name!, out var formValue))
                {
                    var convertedValue = ConvertValueStringToType(prop.PropertyType, formValue);
                    prop.SetValue(instance, convertedValue);
                }
            }

            return instance!;
        }
    }
}
