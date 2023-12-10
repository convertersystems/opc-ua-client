// Copyright (c) Converter Systems LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Workstation.Collections
{
    /// <summary>
    /// Manages validation errors for an object, notifying when the error state changes.
    /// </summary>
    /// <typeparam name="T">The type of the error object.</typeparam>
    public class ErrorsContainer<T>
    {
        private static readonly T[] _noErrors = Array.Empty<T>();
        private readonly Action<string> _raiseErrorsChanged;
        private readonly Dictionary<string, List<T>> _validationResults;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorsContainer{T}"/> class.
        /// </summary>
        /// <param name="raiseErrorsChanged">The action that invoked if when errors are added for an object./>
        /// event.</param>
        public ErrorsContainer(Action<string> raiseErrorsChanged)
        {

            if (raiseErrorsChanged == null)
            {
                throw new ArgumentNullException(nameof(raiseErrorsChanged));
            }

            _raiseErrorsChanged = raiseErrorsChanged;
            _validationResults = new Dictionary<string, List<T>>();
        }

        /// <summary>
        /// Gets a value indicating whether the object has validation errors.
        /// </summary>
        public bool HasErrors
        {
            get
            {
                return _validationResults.Count != 0;
            }
        }

        /// <summary>
        /// Gets the validation errors for a specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property.</param>
        /// <returns>The validation errors of type <typeparamref name="T"/> for the property.</returns>
        public IEnumerable<T> GetErrors(string? propertyName)
        {
            var localPropertyName = propertyName ?? string.Empty;
            List<T>? currentValidationResults = null;
            if (_validationResults.TryGetValue(localPropertyName, out currentValidationResults))
            {
                return currentValidationResults;
            }
            else
            {
                return _noErrors;
            }
        }

        /// <summary>
        /// Clears the errors for a property.
        /// </summary>
        /// <param name="propertyName">The name of th property for which to clear errors.</param>
        /// <example>
        ///     container.ClearErrors("SomeProperty");
        /// </example>
        public void ClearErrors(string propertyName)
        {
            SetErrors(propertyName, new List<T>());
        }

        /// <summary>
        /// Sets the validation errors for the specified property.
        /// </summary>
        /// <remarks>
        /// If a change is detected then the errors changed event is raised.
        /// </remarks>
        /// <param name="propertyName">The name of the property.</param>
        /// <param name="newValidationResults">The new validation errors.</param>
        public void SetErrors(string propertyName, IEnumerable<T>? newValidationResults)
        {
            var localPropertyName = propertyName ?? string.Empty;
            var hasCurrentValidationResults = _validationResults.ContainsKey(localPropertyName);
            var hasNewValidationResults = newValidationResults != null && newValidationResults.Count() > 0;

            if (hasCurrentValidationResults || hasNewValidationResults)
            {
                if (hasNewValidationResults)
                {
                    _validationResults[localPropertyName] = new List<T>(newValidationResults!);
                    _raiseErrorsChanged(localPropertyName);
                }
                else
                {
                    _validationResults.Remove(localPropertyName);
                    _raiseErrorsChanged(localPropertyName);
                }
            }
        }
    }
}
