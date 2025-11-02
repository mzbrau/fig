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
            await JsRuntime.InvokeVoidAsync("MonacoIntegration.resize", "jsoneditor-large");
            
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
                            await JsRuntime.InvokeVoidAsync("MonacoIntegration.resize", "jsoneditor-large");
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
            var editorId = "jsoneditor-large";
            var options = new
            {
                value = InitialValue,
                language = "json",
                theme = "vs-dark",
                readOnly = Setting.IsReadOnly,
                jsonSchema = Setting.JsonSchemaString,
                automaticLayout = true,
                isDialog = true // Explicitly mark as dialog editor
            };

            await JsRuntime.InvokeVoidAsync("MonacoIntegration.initialize", editorId, options);
            
            // Set up change event listener
            await JsRuntime.InvokeVoidAsync("MonacoIntegration.onDidChangeModelContent", editorId, 
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
                
                // Force UI update to reflect validation state
                await InvokeAsync(StateHasChanged);
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
            return await JsRuntime.InvokeAsync<string>("MonacoIntegration.getValue", "jsoneditor-large");
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
            await JsRuntime.InvokeVoidAsync("MonacoIntegration.setValue", "jsoneditor-large", value ?? "");
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
        
        // Trigger validation and UI update
        Setting.ValueChanged(Setting.Value ?? "");
        await OnValueChanged.InvokeAsync(Setting.Value ?? "");
        StateHasChanged();
    }

    private async Task FormatJson()
    {
        if (Setting.IsReadOnly) return;

        try
        {
            await JsRuntime.InvokeVoidAsync("MonacoIntegration.formatDocument", "jsoneditor-large");
            
            // Get the formatted value and update the setting
            var formattedValue = await GetEditorValue();
            Setting.Value = formattedValue;
            Setting.ValueChanged(formattedValue);
            await OnValueChanged.InvokeAsync(formattedValue);
            
            // Force UI update to reflect validation state
            StateHasChanged();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error formatting JSON: {ex.Message}");
        }
    }

    private async Task ToggleSchema()
    {
        ShowSchema = !ShowSchema;
        StateHasChanged();
        
        if (ShowSchema && !string.IsNullOrEmpty(Setting.JsonSchemaString))
        {
            // Wait longer for DOM update and Blazor rendering
            await Task.Delay(200); 
            
            try
            {
                var schemaId = "schema-editor";
                var options = new
                {
                    value = Setting.JsonSchemaString,
                    language = "json",
                    theme = "vs-dark",
                    readOnly = true,
                    automaticLayout = true,
                    isDialog = true // Schema editor in dialog is also a dialog editor
                };
                
                // Initialize Monaco editor for schema
                await JsRuntime.InvokeVoidAsync("MonacoIntegration.initialize", schemaId, options);
                
                // Wait a bit more to ensure Monaco editor is fully initialized
                await Task.Delay(100); 
                
                // Initialize resizable splitter functionality
                await JsRuntime.InvokeVoidAsync("JsonEditorDialog.setupSchemaResize", 
                    "json-editor-dialog", "jsoneditor-large-container", "schema-section");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting up schema: {ex.Message}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_isInitialized)
            {
                await JsRuntime.InvokeVoidAsync("MonacoIntegration.dispose", "jsoneditor-large");
                if (ShowSchema)
                {
                    await JsRuntime.InvokeVoidAsync("MonacoIntegration.dispose", "schema-editor");
                }
                await JsRuntime.InvokeVoidAsync("JsonEditorDialog.cleanup");
            }
        }
        catch
        {
            // Ignore errors during disposal
        }
    }
}