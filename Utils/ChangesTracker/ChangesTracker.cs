using System.Linq;
using System.Dynamic;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WildHealth.Shared.Data.ChangesTracker;
using WildHealth.Shared.Data.Entities;
using WildHealth.Shared.Data.Models;
using Newtonsoft.Json;

namespace WildHealth.Application.Utils.ChangesTracker;

/// <summary>
/// <see cref="IChangesTracker"/>
/// </summary>
public class ChangesTracker : IChangesTracker
{
    /// <summary>
    /// <see cref="IChangesTracker.Track"/>
    /// </summary>
    /// <param name="entries"></param>
    /// <returns></returns>
    public ChangesModel[] Track(EntityEntry[] entries)
    {
        var allChanges = new List<ChangesModel>();
        
        foreach (var entry in entries)
        {
            var changes = entry.State switch
            {
                EntityState.Added => GetInsertedEntityChanges(entry),
                EntityState.Modified => GetModifiedEntityChanges(entry),
                EntityState.Deleted => GetDeletedEntityChanges(entry),
                _ => null
            };

            if (changes is not null)
            {
                allChanges.Add(changes);
            }
        }

        return allChanges.ToArray();
    }
    
    #region private

    /// <summary>
    /// Returns modified properties
    /// </summary>
    /// <param name="entry"></param>
    private ChangesModel GetModifiedEntityChanges(EntityEntry entry)
    {
        var originalExpando = new ExpandoObject();
        var currentExpando = new ExpandoObject();
        var entity = entry.Metadata.Name;
        var originalValues = entry.OriginalValues;
        var currentValues = entry.CurrentValues;
    
        foreach (var property in originalValues.Properties)
        {
            var originalValue = originalValues[property.Name];
            var currentValue = currentValues[property.Name];

            if (!Equals(originalValue, currentValue))
            {
                AddProperty(
                    expando: originalExpando, 
                    propertyName: property.Name, 
                    propertyValue: originalValue?.ToString()
                );
            
                AddProperty(
                    expando: currentExpando, 
                    propertyName: property.Name, 
                    propertyValue: currentValue?.ToString()
                );
            }
        }

        return new ChangesModel
        {
            Entity = entity,
            Key = GetKey(entry),
            CurrentValue = JsonConvert.SerializeObject(currentExpando),
            OriginalValue = JsonConvert.SerializeObject(originalExpando),
            State = entry.State.ToString()
        };
    }
    
    /// <summary>
    /// Returns inserted properties
    /// </summary>
    /// <param name="entry"></param>
    private ChangesModel GetInsertedEntityChanges(EntityEntry entry)
    {
        var originalExpando = new ExpandoObject();
        var currentExpando = new ExpandoObject();
        var entity = entry.Metadata.Name;
        var originalValues = entry.OriginalValues;
        var currentValues = entry.CurrentValues;
        
        foreach (var property in currentValues.Properties)
        {
            var originalValue = originalValues[property.Name];
            var currentValue = currentValues[property.Name];
        
            AddProperty(
                expando: originalExpando, 
                propertyName: property.Name, 
                propertyValue: originalValue?.ToString()
            );
            
            AddProperty(
                expando: currentExpando, 
                propertyName: property.Name, 
                propertyValue: currentValue?.ToString()
            );
        }

        return new ChangesModel
        {
            Entity = entity,
            Key = GetKey(entry),
            CurrentValue = JsonConvert.SerializeObject(currentExpando),
            OriginalValue = JsonConvert.SerializeObject(originalExpando),
            State = entry.State.ToString()
        };
    }
    
    /// <summary>
    /// Returns deleted properties
    /// </summary>
    /// <param name="entry"></param>
    private ChangesModel GetDeletedEntityChanges(EntityEntry entry)
    {
        var entity = entry.Metadata.Name;
        var originalValues = entry.OriginalValues;
        var expando = new ExpandoObject();
        
        foreach (var property in originalValues.Properties)
        {
            var value = originalValues[property.Name];

            AddProperty(
                expando: expando, 
                propertyName: property.Name, 
                propertyValue: value?.ToString()
            );
        }

        return new ChangesModel
        {
            Entity = entity,
            Key = GetKey(entry),
            OriginalValue = JsonConvert.SerializeObject(expando),
            State = entry.State.ToString()
        };
    }

    /// <summary>
    /// Returns entity key
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    private string? GetKey(EntityEntry entry)
    {
        if (entry.Entity is BaseEntity baseEntity)
        {
            return baseEntity.Id.ToString();
        }

        var name = entry.CurrentValues.Properties.FirstOrDefault(x => x.IsForeignKey())?.Name;
        
        return string.IsNullOrEmpty(name)
            ? string.Empty
            : entry.CurrentValues[name]?.ToString();
    }
    
    /// <summary>
    /// Add new property to expando object
    /// </summary>
    /// <param name="expando"></param>
    /// <param name="propertyName"></param>
    /// <param name="propertyValue"></param>
    private static void AddProperty(ExpandoObject expando, string propertyName, object? propertyValue)
    {
        var expandoDict = expando as IDictionary<string, object?>;
        
        if (expandoDict.ContainsKey(propertyName))
        {
            expandoDict[propertyName] = propertyValue;
        }
        else
        {
            expandoDict.Add(propertyName, propertyValue);
        }
    }
    
    #endregion
}