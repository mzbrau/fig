using System;
using System.Linq.Expressions;

namespace Fig.Client.Abstractions.StatusProperties
{
    public interface IFigStatusProperties<T> where T : class, new()
    {
        /// <summary>
        /// Current in-memory status properties (thread-safe snapshot copy semantics for reads via Current).
        /// </summary>
        T Current { get; }

        /// <summary>
        /// Sets a property value. When <paramref name="textColor"/> is non-null, updates the
        /// optional Fig.Web text color (#RGB or #RRGGBB). When null (default), any existing
        /// color for the property is left unchanged.
        /// </summary>
        void Set<TValue>(Expression<Func<T, TValue>> property, TValue value, string? textColor = null);

        /// <summary>
        /// Sets or clears the text colour for a property without changing its value.
        /// Pass null to clear the colour.
        /// </summary>
        void SetTextColor<TValue>(Expression<Func<T, TValue>> property, string? textColor);

        void Update(Action<T> update);

        void Clear<TValue>(Expression<Func<T, TValue>> property);
    }
}
