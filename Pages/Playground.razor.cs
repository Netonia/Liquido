using BlazorMonaco.Editor;
using Microsoft.JSInterop;

namespace Liquido.Pages;
public partial class Playground
{
    private StandaloneCodeEditor? _jsonEditor;
    private StandaloneCodeEditor? _liquidEditor;
    private StandaloneCodeEditor? _previewEditor;
    private string _selectedLanguage = "plaintext";
    private string _selectedExample = "";
    private bool _isRendering = false;
    private string? _error;
    private System.Timers.Timer? _debounceTimer;
    private bool _jsonEditorReady = false;
    private bool _liquidEditorReady = false;
    private bool _previewEditorReady = false;
    private bool _monacoLoaded = false;
    private bool _copied = false;
    private System.Threading.CancellationTokenSource? _copiedCts;
    private string _theme = "vs-dark";

    private string SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            _selectedLanguage = value;
            _ = UpdatePreviewLanguageAsync();
        }
    }

    private string SelectedExample
    {
        get => _selectedExample;
        set
        {
            _selectedExample = value;
            _ = LoadExampleAsync(value);
        }
    }

    private string Theme
    {
        get => _theme;
        set
        {
            _theme = value;
            _ = UpdateThemeAsync(value);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Load theme from localStorage
            try
            {
                var savedTheme = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "liquido-theme");
                if (!string.IsNullOrEmpty(savedTheme))
                {
                    _theme = savedTheme;
                }
            }
            catch
            {
                // If localStorage fails, use default theme
            }

            // Wait for Monaco to be fully loaded
            await Task.Delay(1000);
            _monacoLoaded = true;
            StateHasChanged();
        }
    }

    private async Task LoadExampleAsync(string exampleId)
    {
        if (string.IsNullOrEmpty(exampleId) || _jsonEditor == null || _liquidEditor == null)
            return;

        var (json, liquid, language) = GetExampleContent(exampleId);

        await _jsonEditor.SetValue(json);
        await _liquidEditor.SetValue(liquid);

        // Update preview language if example specifies one
        if (!string.IsNullOrEmpty(language))
        {
            _selectedLanguage = language;
            await UpdatePreviewLanguageAsync();
        }

        // Trigger a render
        await RenderTemplateAsync();
    }

    private (string json, string liquid, string language) GetExampleContent(string exampleId)
    {
        return exampleId switch
        {
            "array" => (
              @"[
  { ""type"": ""string"", ""name"": ""FirstName"" },
  { ""type"": ""string"", ""name"": ""LastName"" },
  { ""type"": ""int"", ""name"": ""Age"" },
  { ""type"": ""bool"", ""name"": ""IsActive"" }
]",
             @"public class Person
{
{%- for prop in model %}
    public {{ prop.type }} {{ prop.name }} { get; set; }
{% endfor %}
}",
                 "csharp"
                    ),

            "sql" => (
 @"[
  {
    ""table"": ""Users"",
    ""columns"": [""Id"", ""Name"", ""Email""],
    ""values"": [1, ""John Doe"", ""john@example.com""]
  },
  {
    ""table"": ""Users"",
    ""columns"": [""Id"", ""Name"", ""Email""],
    ""values"": [2, ""Jane Smith"", ""jane@example.com""]
  }
]",
    @"{% for row in model %}INSERT INTO {{ row.table }} ({{ row.columns | join: ', ' }})
VALUES ({{ row.values | join: ', ' }});
{% endfor %}",
    "sql"
            ),

            "csharp" => (
   @"{
  ""namespace"": ""MyApp.Models"",
  ""className"": ""Product"",
  ""properties"": [
    { ""name"": ""Id"", ""type"": ""int"" },
    { ""name"": ""Name"", ""type"": ""string"" },
    { ""name"": ""Price"", ""type"": ""decimal"" },
    { ""name"": ""InStock"", ""type"": ""bool"" }
  ]
}",
          @"namespace {{ namespace }}
{
    public class {{ className }}
    {
{%- for prop in properties %}
        public {{ prop.type }} {{ prop.name }} { get; set; }{% endfor %}
    }
}",
          "csharp"
            ),

            "html" => (
     @"{
  ""title"": ""User List"",
  ""users"": [
    { ""id"": 1, ""name"": ""John Doe"", ""email"": ""john@example.com"" },
    { ""id"": 2, ""name"": ""Jane Smith"", ""email"": ""jane@example.com"" },
    { ""id"": 3, ""name"": ""Bob Johnson"", ""email"": ""bob@example.com"" }
  ]
}",
         @"<h1>{{ title }}</h1>
<table>
  <thead>
  <tr>
      <th>ID</th>
      <th>Name</th>
      <th>Email</th>
    </tr>
  </thead>
  <tbody>
{%- for user in users %}
    <tr>
      <td>{{ user.id }}</td>
      <td>{{ user.name }}</td>
      <td>{{ user.email }}</td>
    </tr>
{% endfor %}
  </tbody>
</table>",
 "html"
   ),

            _ => ("", "", "")
        };
    }

    private StandaloneEditorConstructionOptions JsonEditorOptions(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            Language = "json",
            Theme = _theme,
            Value = @"{
  ""firstName"": ""John"",
  ""lastName"": ""Doe""
}",
            AutomaticLayout = true,
            Minimap = new EditorMinimapOptions { Enabled = false },
            ScrollBeyondLastLine = false
        };
    }

    private StandaloneEditorConstructionOptions LiquidEditorOptions(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            Language = "liquid",
            Theme = _theme,
            Value = @"Hello, {{ firstName }} {{ lastName }}!",
            AutomaticLayout = true,
            Minimap = new EditorMinimapOptions { Enabled = false },
            ScrollBeyondLastLine = false
        };
    }

    private StandaloneEditorConstructionOptions PreviewEditorOptions(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            Language = _selectedLanguage,
            Theme = _theme,
            Value = "",
            ReadOnly = true,
            AutomaticLayout = true,
            Minimap = new EditorMinimapOptions { Enabled = false },
            ScrollBeyondLastLine = false
        };
    }

    private async Task OnJsonEditorInit()
    {
        _jsonEditorReady = true;
        await TryInitialRender();
    }

    private async Task OnLiquidEditorInit()
    {
        _liquidEditorReady = true;
        await TryInitialRender();
    }

    private async Task OnPreviewEditorInit()
    {
        _previewEditorReady = true;
        await TryInitialRender();
    }

    private async Task TryInitialRender()
    {
        // Only render once all editors are ready
        if (_jsonEditorReady && _liquidEditorReady && _previewEditorReady)
        {
            await RenderTemplateAsync();
        }
    }

    private void OnJsonChanged(ModelContentChangedEvent e)
    {
        OnEditorContentChanged();
    }

    private void OnLiquidChanged(ModelContentChangedEvent e)
    {
        OnEditorContentChanged();
    }

    private void OnEditorContentChanged()
    {
        _debounceTimer?.Stop();
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Timers.Timer(500); // 500ms debounce
        _debounceTimer.Elapsed += async (sender, e) =>
        {
            _debounceTimer?.Stop();
            await InvokeAsync(async () =>
            {
                await RenderTemplateAsync();
                StateHasChanged();
            });
        };
        _debounceTimer.AutoReset = false;
        _debounceTimer.Start();
    }

    private async Task RenderTemplateAsync()
    {
        if (_jsonEditor == null || _liquidEditor == null || _previewEditor == null)
            return;

        try
        {
            _isRendering = true;
            _error = null;
            StateHasChanged();

            var jsonData = await _jsonEditor.GetValue();
            var liquidTemplate = await _liquidEditor.GetValue();

            // Call client-side rendering service
            var (success, result, error) = await RenderService.RenderAsync(jsonData, liquidTemplate);

            if (success && result != null)
            {
                await _previewEditor.SetValue(result);
                _error = null;
            }
            else
            {
                _error = error ?? "Unknown error";
            }
        }
        catch (Exception ex)
        {
            _error = $"Error: {ex.Message}";
        }
        finally
        {
            _isRendering = false;
            StateHasChanged();
        }
    }

    private async Task UpdatePreviewLanguageAsync()
    {
        if (_previewEditor != null && _previewEditorReady)
        {
            try
            {
                var currentValue = await _previewEditor.GetValue();
                await _previewEditor.UpdateOptions(new EditorUpdateOptions
                {
                    Theme = _theme
                });

                // Update the model language
                var model = await _previewEditor.GetModel();
                if (model != null)
                {
                    await Global.SetModelLanguage(JSRuntime, model, _selectedLanguage);
                }
            }
            catch (Exception ex)
            {
                // Ignore errors during language update
                Console.WriteLine($"Error updating preview language: {ex.Message}");
            }
        }
    }

    private async Task CopyToClipboard()
    {
        if (_previewEditor == null || !_previewEditorReady)
            return;

        try
        {
            var content = await _previewEditor.GetValue();

            if (!string.IsNullOrEmpty(content))
            {
                await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", content);

                // Show "Copied!" feedback for 2 seconds
                _copiedCts?.Cancel();
                _copiedCts = new System.Threading.CancellationTokenSource();
                _copied = true;
                StateHasChanged();

                try
                {
                    await Task.Delay(2000, _copiedCts.Token);
                    _copied = false;
                    StateHasChanged();
                }
                catch (TaskCanceledException)
                {
                    // Expected when user copies again before timer completes
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error copying to clipboard: {ex.Message}");
            // Could show an error message to the user here
        }
    }

    private async Task UpdateThemeAsync(string theme)
    {
        // Save to localStorage
        try
        {
            await JSRuntime.InvokeVoidAsync("localStorage.setItem", "liquido-theme", theme);
        }
        catch
        {
            // Ignore localStorage errors
        }

        // Update all editors
        if (_jsonEditor != null && _jsonEditorReady)
        {
            await _jsonEditor.UpdateOptions(new EditorUpdateOptions { Theme = theme });
        }

        if (_liquidEditor != null && _liquidEditorReady)
        {
            await _liquidEditor.UpdateOptions(new EditorUpdateOptions { Theme = theme });
        }

        if (_previewEditor != null && _previewEditorReady)
        {
            await _previewEditor.UpdateOptions(new EditorUpdateOptions { Theme = theme });
        }

        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        _debounceTimer?.Dispose();
        _copiedCts?.Cancel();
        _copiedCts?.Dispose();
    }
}