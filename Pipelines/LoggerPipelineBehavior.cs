using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MediatR;
using WildHealth.Shared.Exceptions;

namespace WildHealth.Application.Pipelines
{
    public class LoggerPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
    {
        private readonly string _commandType = typeof(TRequest).Name;
        private readonly ILogger<LoggerPipelineBehavior<TRequest, TResponse>> _logger;

        public LoggerPipelineBehavior(ILogger<LoggerPipelineBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest command, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _logger.LogInformation("[MediatR] Started processing command: {CommandType}", _commandType);

            var sw = Stopwatch.StartNew();
            try
            {
                return await next();
            }
            catch (AppException appEx)
            {
                if (appEx.LogAsError)
                {
                    _logger.LogError(appEx, 
                        "[MediatR] Error during MediatR command: {CommandType}. {ExceptionMessage}.", 
                        _commandType,
                        appEx.Message);
                    
                }
                else
                {
                     _logger.LogInformation(appEx, 
                                            "[MediatR] Exception during MediatR command: {CommandType}. {ExceptionMessage}.", 
                                            _commandType,
                                            appEx.Message);
                }

                throw;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, 
                    "[MediatR] Error during MediatR command: {CommandType}. {ExceptionMessage}.", 
                    _commandType,
                    ex.Message);
                
                throw;
            }            
            finally
            {
                sw.Stop();
                _logger.LogInformation(
                    "[MediatR] Finished processing command: {CommandType}; Time spent: {TimeSpent} sec",
                    _commandType,
                    Math.Round(sw.Elapsed.TotalSeconds, 3));
            }
        }
    }
}