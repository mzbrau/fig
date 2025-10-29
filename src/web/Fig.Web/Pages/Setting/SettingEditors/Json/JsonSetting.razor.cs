using System.Reactive.Linq;
using System.Reactive.Subjects;
using Fig.Web.Models.Setting.ConfigurationModels;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen;

namespace Fig.Web.Pages.Setting.SettingEditors.Json;

public partial class JsonSetting
{
    [Parameter]
    public JsonSettingConfigurationModel Setting { get; set; } = null!;
    
    [Inject]
    public DialogService DialogService { get; set; } = null!;
    
    [Inject]
    public IJSRuntime JsRuntime { get; set; } = null!;

    private bool ShowSchema { get; set; }
    private bool _isInitialized;
    private string? _lastValue;
    private DotNetObjectReference<JsonSetting>? _dotNetRef;
    
    // Reactive validation using System.Reactive
    private readonly Subject<string> _validationSubject = new();
    private IDisposable? _validationSubscription;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_isInitialized)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await Task.Delay(100); // Allow DOM to settle
            await InitializeEditor();
            
            // Set up reactive validation with throttling (like in Settings.razor.cs)
            _validationSubscription = _validationSubject
                .Throttle(TimeSpan.FromMilliseconds(200))
                .Subscribe(value => {
                    PerformValidation(value);
                    InvokeAsync(StateHasChanged);
                });
        }
    }

    private async Task InitializeEditor()
    {
        try
        {
            var editorId = $"json-editor-{Setting.Name}";
            var options = new
            {
                value = Setting.Value ?? "",
                language = "json",
                theme = "vs-dark",
                readOnly = Setting.IsReadOnly,
                jsonSchema = Setting.JsonSchemaString,
                automaticLayout = false
            };

            await JsRuntime.InvokeVoidAsync("monacoIntegration.initialize", editorId, options);
            
            // Set up change event listener
            await JsRuntime.InvokeVoidAsync("monacoIntegration.onDidChangeModelContent", editorId, 
                DotNetObjectReference.Create(this), nameof(OnContentChanged));
            
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing Monaco editor: {ex.Message}");
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
                Setting.Value = currentValue;
                
                // Trigger reactive validation
                _validationSubject.OnNext(currentValue);
                
                Console.WriteLine($"Small editor content changed: {currentValue?.Length} characters");
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
            return await JsRuntime.InvokeAsync<string>("monacoIntegration.getValue", $"json-editor-{Setting.Name}");
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
            if (_isInitialized)
            {
                await JsRuntime.InvokeVoidAsync("monacoIntegration.setValue", $"json-editor-{Setting.Name}", value ?? "");
                _lastValue = value;
                Console.WriteLine($"Set small editor value: {value?.Length} characters");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting editor value: {ex.Message}");
        }
    }

    private async Task OnShowSchema()
    {
        ShowSchema = !ShowSchema;
        StateHasChanged();
        
        if (ShowSchema && !string.IsNullOrEmpty(Setting.JsonSchemaString))
        {
            await Task.Delay(100); // Wait for DOM update
            var schemaId = $"schema-editor-{Setting.Name}";
            var options = new
            {
                value = Setting.JsonSchemaString,
                language = "json",
                theme = "vs-dark",
                readOnly = true
            };
            await JsRuntime.InvokeVoidAsync("monacoIntegration.initialize", schemaId, options);
        }
    }

    private async Task GenerateJson()
    {
        if (Setting.IsReadOnly) return;
        
        Setting.GenerateJson();
        await SetEditorValue(Setting.Value ?? "");
        
        // Trigger validation after generating JSON
        Setting.ValueChanged(Setting.Value ?? "");
        
        // Also trigger reactive validation
        _validationSubject.OnNext(Setting.Value ?? "");
        
        // Force UI update
        StateHasChanged();
        
        // Refresh editor layout after a short delay
        await Task.Delay(50);
        await JsRuntime.InvokeVoidAsync("monacoIntegration.resize", $"json-editor-{Setting.Name}");
    }

    private async Task FormatJson()
    {
        if (Setting.IsReadOnly) return;

        try
        {
            await JsRuntime.InvokeVoidAsync("monacoIntegration.formatDocument", $"json-editor-{Setting.Name}");
            
            // Get the formatted value and update the setting
            var formattedValue = await GetEditorValue();
            Setting.Value = formattedValue;
            Setting.ValueChanged(formattedValue);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error formatting JSON: {ex.Message}");
        }
    }

    private async Task OpenExpandedEditor()
    {
        try
        {
            // Get current value from the small editor
            var currentValue = await GetEditorValue();
            
            // Show dialog with a render fragment that creates the component
            var dialogTask = DialogService.OpenAsync($"JSON Editor - {Setting.Name}", ds => builder =>
            {
                // Create the JsonEditorDialog component using manual type reference
                var componentType = Type.GetType("Fig.Web.Pages.Setting.SettingEditors.Json.JsonEditorDialog, Fig.Web");
                if (componentType != null)
                {
                    builder.OpenComponent(0, componentType);
                    builder.AddAttribute(1, "Setting", Setting);
                    builder.AddAttribute(2, "InitialValue", currentValue);
                    builder.AddAttribute(3, "OnValueChanged", EventCallback.Factory.Create<string>(this, OnDialogValueChanged));
                    builder.CloseComponent();
                }
                else
                {
                    builder.AddContent(0, "Error: Could not load dialog component");
                }
            },
            new DialogOptions() 
            { 
                Width = "95vw", 
                Height = "90vh", 
                Resizable = true, 
                Draggable = true,
                CloseDialogOnOverlayClick = false,
                ShowClose = true,
                ShowTitle = true
            });
            
            // Wait for dialog to complete
            await dialogTask;
            
            Console.WriteLine("Dialog closed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error opening expanded editor: {ex.Message}");
        }
    }

    private async Task OnDialogValueChanged(string newValue)
    {
        try
        {
            _lastValue = newValue;
            Setting.Value = newValue;
            
            // Update the small editor immediately
            await SetEditorValue(newValue);
            
            // Trigger reactive validation
            _validationSubject.OnNext(newValue);
            
            Console.WriteLine($"Dialog value changed and synced: {newValue?.Length} characters");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling dialog value change: {ex.Message}");
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_isInitialized && Setting.Value != _lastValue)
        {
            await SetEditorValue(Setting.Value ?? "");
        }
    }

    private void PerformValidation(string value)
    {
        try
        {
            Setting.ValueChanged(value);
            Console.WriteLine($"Reactive validation performed for: {value.Length} characters");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during reactive validation: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            // Dispose reactive subscription
            _validationSubscription?.Dispose();
            _validationSubject?.Dispose();
            
            if (_isInitialized)
            {
                await JsRuntime.InvokeVoidAsync("monacoIntegration.dispose", $"json-editor-{Setting.Name}");
                if (ShowSchema)
                {
                    await JsRuntime.InvokeVoidAsync("monacoIntegration.dispose", $"schema-editor-{Setting.Name}");
                }
            }
            _dotNetRef?.Dispose();
        }
        catch
        {
            // Ignore errors during disposal
        }
    }
}