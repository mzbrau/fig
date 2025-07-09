using System;
using System.Collections.Generic;
using System.Linq;

namespace Fig.Client.CustomActions;

public static class ResultBuilder
{
    public static CustomActionResultModel CreateSuccessResult(string name)
    {
        return new CustomActionResultModel(name, true);
    }

    public static CustomActionResultModel CreateFailureResult(string name)
    {
        return new CustomActionResultModel(name, false);
    }
    
    public static CustomActionResultModel WithTextResult(this CustomActionResultModel model, string textResult)
    {
        if (!string.IsNullOrEmpty(model.TextResult))
            throw new InvalidOperationException("Text result already set");
        
        model.TextResult = textResult;
        return model;
    }
    
    public static CustomActionResultModel WithDataGridResult<T>(this CustomActionResultModel model, IEnumerable<T> dataGridResult) where T : class
    {
        if (model.DataGridResult != null)
            throw new InvalidOperationException("Data grid result already set");

        if (dataGridResult is List<Dictionary<string, object?>> list)
            model.DataGridResult = list;
        else
            model.DataGridResult = ConvertToDataGridResult(dataGridResult);
        
        return model;
    }

    private static List<Dictionary<string, object?>> ConvertToDataGridResult<T>(IEnumerable<T> dataGridResult) where T : class
    {
        var properties = typeof(T).GetProperties();
        var columns = properties.Select(p => p.Name).ToList();

        List<Dictionary<string, object?>> result = [];
        foreach (var item in dataGridResult)
        {
            var row = new Dictionary<string, object?>();
            foreach (var column in columns)
            {
                var value = properties.FirstOrDefault(a => a.Name == column)?.GetValue(item)?.ToString() ?? string.Empty;
                row[column] = value;
            }
            
            result.Add(row);
        }
        
        return result;
    }
}