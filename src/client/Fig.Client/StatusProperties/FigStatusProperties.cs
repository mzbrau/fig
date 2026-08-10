using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Fig.Client.Abstractions.StatusProperties;
using Fig.Contracts.Status;
using Microsoft.Extensions.Logging;

namespace Fig.Client.StatusProperties
{
    internal sealed class FigStatusProperties<T> : IFigStatusProperties<T> where T : class, new()
    {
        private readonly object _gate = new();
        private readonly ILogger<FigStatusProperties<T>>? _logger;
        private readonly Dictionary<string, string> _textColors = new(StringComparer.Ordinal);
        private T _current = new();

        public FigStatusProperties(ILogger<FigStatusProperties<T>>? logger = null)
        {
            _logger = logger;
        }

        public T Current
        {
            get
            {
                lock (_gate)
                {
                    return Clone(_current);
                }
            }
        }

        public void Set<TValue>(Expression<Func<T, TValue>> property, TValue value, string? textColor = null)
        {
            if (textColor is not null && !CustomStatusPropertiesValidator.IsValidTextColor(textColor))
                throw new ArgumentException("TextColor must be a hex color (#RGB or #RRGGBB).", nameof(textColor));

            var propertyInfo = GetPropertyInfo(property);
            lock (_gate)
            {
                propertyInfo.SetValue(_current, value);
                if (textColor is not null)
                    _textColors[propertyInfo.Name] = textColor;
            }
        }

        public void SetTextColor<TValue>(Expression<Func<T, TValue>> property, string? textColor)
        {
            if (textColor is not null && !CustomStatusPropertiesValidator.IsValidTextColor(textColor))
                throw new ArgumentException("TextColor must be a hex color (#RGB or #RRGGBB).", nameof(textColor));

            var propertyInfo = GetPropertyInfo(property);
            lock (_gate)
            {
                if (textColor is null)
                    _textColors.Remove(propertyInfo.Name);
                else
                    _textColors[propertyInfo.Name] = textColor;
            }
        }

        public void Update(Action<T> update)
        {
            if (update is null)
                throw new ArgumentNullException(nameof(update));

            lock (_gate)
            {
                update(_current);
            }
        }

        public void Clear<TValue>(Expression<Func<T, TValue>> property)
        {
            var propertyInfo = GetPropertyInfo(property);
            lock (_gate)
            {
                propertyInfo.SetValue(_current, GetDefault(propertyInfo.PropertyType));
                _textColors.Remove(propertyInfo.Name);
            }
        }

        internal CustomStatusPropertiesDataContract? CreateSnapshot()
        {
            T snapshot;
            Dictionary<string, string>? textColors;
            lock (_gate)
            {
                snapshot = Clone(_current);
                textColors = _textColors.Count == 0
                    ? null
                    : new Dictionary<string, string>(_textColors, StringComparer.Ordinal);
            }

            return CustomStatusPropertiesSerializer.TryCreateSnapshot(snapshot, _logger, textColors);
        }

        private static PropertyInfo GetPropertyInfo<TValue>(Expression<Func<T, TValue>> property)
        {
            if (property.Body is MemberExpression { Member: PropertyInfo propertyInfo })
                return propertyInfo;

            if (property.Body is UnaryExpression { Operand: MemberExpression { Member: PropertyInfo unaryProperty } })
            {
                return unaryProperty;
            }

            throw new ArgumentException("Expression must be a property access.", nameof(property));
        }

        private static object? GetDefault(Type type)
        {
            return type.IsValueType ? Activator.CreateInstance(type) : null;
        }

        private static T Clone(T source)
        {
            // Shallow copy is enough: only scalar properties are supported.
            var clone = new T();
            foreach (var property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0)
                    continue;

                property.SetValue(clone, property.GetValue(source));
            }

            return clone;
        }
    }
}
