﻿using IdempotentAPI.AccessCache;
using IdempotentAPI.Core;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace IdempotentAPI.Filters
{
    public class IdempotencyAttributeFilter : IActionFilter, IResultFilter
    {
        private readonly IIdempotencyAccessCache _distributedCache;
        private readonly IIdempotencySettings _settings;
        private readonly ILogger<Idempotency> _logger;

        private Idempotency? _idempotency = null;

        public IdempotencyAttributeFilter(
            IIdempotencyAccessCache distributedCache,
            IIdempotencySettings settings,
            ILogger<Idempotency> logger)
        {
            _distributedCache = distributedCache;
            _settings = settings;
            _logger = logger;
        }

        /// <summary>
        /// Runs before the execution of the controller
        /// </summary>
        /// <param name="context"></param>
        public void OnActionExecuting(ActionExecutingContext context)
        {
            // If the Idempotency is disabled then stop
            if (!_settings.Enabled)
            {
                return;
            }

            // Initialize only on its null (in case of multiple executions):
            if (_idempotency == null)
            {
                _idempotency = new Idempotency(_distributedCache, _settings, _logger);
            }

            _idempotency.ApplyPreIdempotency(context);
        }

        /// <summary>
        /// Runs after the execution of the controller. In our case we used it to perform specific actions on Exceptions.
        /// </summary>
        /// <param name="context"></param>
        public void OnActionExecuted(ActionExecutedContext context)
        {
            // If the Idempotency is disabled then stop.
            // Stop if the PreIdempotency step is not applied.
            if (!_settings.Enabled || _idempotency == null)
            {
                return;
            }

            if (context?.Exception is not null)
            {
                _idempotency.CancelIdempotency();
            }
        }

        // NOT USED
        public void OnResultExecuting(ResultExecutingContext context)
        {

        }

        /// <summary>
        /// Runs after the results have been calculated
        /// </summary>
        /// <param name="context"></param>
        public void OnResultExecuted(ResultExecutedContext context)
        {
            // If the Idempotency is disabled then stop
            if (!_settings.Enabled)
            {
                return;
            }

            // Stop if the PreIdempotency step is not applied:
            if (_idempotency == null)
            {
                return;
            }

            _idempotency.ApplyPostIdempotency(context);
        }
    }
}
