using Fig.Web.Models.Setting.ConfigurationModels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Fig.Web.Pages.Setting.SettingEditors.Json;

public partial class JsonEditorDialog
{
    [Parameter] 
    public JsonSettingConfigurationModel Setting { get; set; } = null!;
    
    [Parameter] 
    public string InitialValue { get; set; } = "";
    
    [Parameter] 
    public EventCallback<string> OnValueChanged { get; set; }
    
    [Inject]
    public IJSRuntime JsRuntime { get; set; } = null!;

    private bool ShowSchema { get; set; }
    private bool _isInitialized;
    private string? _lastValue;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_isInitialized)
        {
            await Task.Delay(200); // Allow DOM to settle
            await InitializeEditor();
            // Trigger layout after a short delay to ensure proper sizing
            await Task.Delay(100);
            await JsRuntime.InvokeVoidAsync("monacoIntegration.resize", $"json-editor-dialog-{Setting.Name}");
            
            // Set up a periodic resize to handle dialog resizing
            _ = Task.Run(async () =>
            {
                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(500);
                    if (_isInitialized)
                    {
                        try
                        {
                            await JsRuntime.InvokeVoidAsync("monacoIntegration.resize", $"json-editor-dialog-{Setting.Name}");
                        }
                        catch
                        {
                            // Ignore errors during periodic resize
                        }
                    }
                }
            });
        }
    }

    private async Task InitializeEditor()
    {
        try
        {
            var editorId = $"json-editor-dialog-{Setting.Name}";
            var options = new
            {
                value = InitialValue,
                language = "json",
                theme = "vs-dark",
                readOnly = Setting.IsReadOnly,
                jsonSchema = Setting.JsonSchemaString,
                automaticLayout = true
            };

            await JsRuntime.InvokeVoidAsync("monacoIntegration.initialize", editorId, options);
            
            // Set up change event listener
            await JsRuntime.InvokeVoidAsync("monacoIntegration.onDidChangeModelContent", editorId, 
                DotNetObjectReference.Create(this), nameof(OnContentChanged));
            
            _isInitialized = true;
            _lastValue = InitialValue;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing Monaco editor dialog: {ex.Message}");
        }
    }

    [JSInvokable]
    public async Task OnContentChanged()
    {
        if (!_isInitialized || Setting.IsReadOnly) return;

        try
        {
            var currentValue = await GetEditorValue();
            if (currentValue != _lastValue)
            {
                _lastValue = currentValue;
                Setting.ValueChanged(currentValue);
                await OnValueChanged.InvokeAsync(currentValue);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling content change: {ex.Message}");
        }
    }

    private async Task<string> GetEditorValue()
    {
        try
        {
            return await JsRuntime.InvokeAsync<string>("monacoIntegration.getValue", $"json-editor-dialog-{Setting.Name}");
        }
        catch
        {
            return "";
        }
    }

    private async Task SetEditorValue(string value)
    {
        try
        {
            await JsRuntime.InvokeVoidAsync("monacoIntegration.setValue", $"json-editor-dialog-{Setting.Name}", value ?? "");
            _lastValue = value;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting editor value: {ex.Message}");
        }
    }

    private async Task GenerateJson()
    {
        if (Setting.IsReadOnly) return;
        
        Setting.GenerateJson();
        await SetEditorValue(Setting.Value ?? "");
    }

    private async Task FormatJson()
    {
        if (Setting.IsReadOnly) return;

        try
        {
            await JsRuntime.InvokeVoidAsync("monacoIntegration.formatDocument", $"json-editor-dialog-{Setting.Name}");
            
            // Get the formatted value and update the setting
            var formattedValue = await GetEditorValue();
            Setting.Value = formattedValue;
            Setting.ValueChanged(formattedValue);
            await OnValueChanged.InvokeAsync(formattedValue);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error formatting JSON: {ex.Message}");
        }
    }

    private async Task ValidateJson()
    {
        try
        {
            var currentValue = await GetEditorValue();
            Setting.ValueChanged(currentValue);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error validating JSON: {ex.Message}");
        }
    }

    private async Task ToggleSchema()
    {
        ShowSchema = !ShowSchema;
        StateHasChanged();
        
        if (ShowSchema && !string.IsNullOrEmpty(Setting.JsonSchemaString))
        {
            await Task.Delay(100); // Wait for DOM update
            var schemaId = $"schema-editor-dialog-{Setting.Name}";
            var options = new
            {
                value = Setting.JsonSchemaString,
                language = "json",
                theme = "vs-dark",
                readOnly = true,
                automaticLayout = true
            };
            await JsRuntime.InvokeVoidAsync("monacoIntegration.initialize", schemaId, options);
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_isInitialized)
            {
                await JsRuntime.InvokeVoidAsync("monacoIntegration.dispose", $"json-editor-dialog-{Setting.Name}");
                if (ShowSchema)
                {
                    await JsRuntime.InvokeVoidAsync("monacoIntegration.dispose", $"schema-editor-dialog-{Setting.Name}");
                }
            }
        }
        catch
        {
            // Ignore errors during disposal
        }
    }
}