using System.Security.Cryptography;
using System.Text;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Pages;

public partial class Utils
{
    private string _plainText = string.Empty;
    private string _encryptedText = string.Empty;

    [Inject] 
    public IClipboardService ClipboardService { get; set; } = default!;
    
    public string PlainText
    {
        get => _plainText;
        set
        {
            if (value != _plainText)
            {
                _plainText = value;
                EncryptedText = EncryptText(_plainText);
            }
        }
    }

    public string EncryptedText
    {
        get => _encryptedText;
        set
        {
            if (value != _encryptedText)
            {

                _encryptedText = value;
                PlainText = DecryptText(_encryptedText);
            }
        }
    }

    private string EncryptText(string plainText)
    {
        var bytes = Encoding.UTF8.GetBytes(plainText);
        var protectedData = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedData);
    }

    private string DecryptText(string encryptedText)
    {
        return Encoding.UTF8.GetString(
            ProtectedData.Unprotect(Convert.FromBase64String(encryptedText), null, DataProtectionScope.CurrentUser));
    }
    
    public void CopyEncryptedText()
    {
        ClipboardService.WriteTextAsync(EncryptedText);
    }

    public void GenerateSecret()
    {
        PlainText = Guid.NewGuid().ToString("N");
    }

    private void OnEncryptedTextChanged(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            EncryptedText = value;
    }

    private void OnPlainTextChanged(string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            PlainText = value;
    }
}