using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WildHealth.IntegrationEvents._Base;

namespace WildHealth.Application.Durable.Chain;

public class ChainOfResponsibility<TPayload>
{
    private string _startStep;
    private bool _isStarted;
    private readonly Queue<(string, Func<TPayload, Task<TPayload>>)> _chain = new();
    private readonly TPayload _payload;
    private readonly LogInformation _logInformation;

    public ChainOfResponsibility(string startStep, TPayload payload, LogInformation logInformation)
    {
        _startStep = startStep;
        _payload = payload;
        _logInformation = logInformation;
    }

    public ChainOfResponsibility<TPayload> Pipe(string stepName, Func<TPayload, Task<TPayload>> chainBlock)
    {
        if (string.IsNullOrEmpty(stepName))
            throw new Exception("Step name must have a value");
        
        if (string.IsNullOrEmpty(_startStep))
            _startStep = stepName;
        
        ThrowIfExists(stepName);
        
        if (stepName == _startStep || _isStarted)
        {
            _chain.Enqueue((stepName, chainBlock));
            _isStarted = true;
        }

        return this;
    }

    public ChainOfResponsibility<TPayload> Pipe(string stepName, Func<TPayload, Task> chainBlock)
    {
        async Task<TPayload> Chain(TPayload e)
        {
            await chainBlock.Invoke(e);
            return _payload; // pass through the initial payload
        }

        return Pipe(stepName, Chain);
    }

    public async Task<TPayload> Run()
    {
        ThrowIfNotExists(_startStep);
        
        var chainingResult = _payload;
        
        while (_chain.Any())
        {
            var (stepName, chainBlock) = _chain.Dequeue();

            try
            {
                _logInformation("Running '{Step}' chain step. {Payload}", stepName, chainingResult);
                ChangeState(chainingResult, stepName);
                chainingResult = await chainBlock.Invoke(chainingResult);
                _logInformation("The chain step '{Step}' executed successfully. {Result}", stepName, chainingResult);
            }
            catch (Exception e)
            {
                // chain failed on step other than where it started
                if (stepName != _startStep)
                    throw new ChainStateChangedException(e.Message, e, stepName, chainingResult!);
                
                throw;
            }
        }
        
        return chainingResult;
    }

    private void ChangeState(TPayload payload, string state)
    {
        if (payload is IDurable durable) 
            durable.State = state;
    }

    private void ThrowIfNotExists(string startStep)
    {
        if (!_chain.Any())
            throw new Exception($"{startStep} not exists");
    }

    private void ThrowIfExists(string stepName)
    {
        if (Exists(stepName))
            throw new Exception($"{stepName} has already been queued up");
    }

    private bool Exists(string stepName) => _chain.Any(c => c.Item1 == stepName);
}

public delegate void LogInformation(string message, params object?[] args);