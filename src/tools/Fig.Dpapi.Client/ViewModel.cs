﻿using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace Fig.Dpapi.Client;

public class ViewModel : INotifyPropertyChanged
{
    private string _plainText = string.Empty;
    private string _encryptedText = string.Empty;
    private bool _suppressEvents;

    public string PlainText
    {
        get => _plainText;
        set
        {
            if (value != _plainText)
            {
                _plainText = value;
                EncryptedText = EncryptText(_plainText);
                if (!_suppressEvents)
                {
                    _suppressEvents = true;
                    OnPropertyChanged();
                    _suppressEvents = false;
                }
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
                if (!_suppressEvents)
                {
                    _suppressEvents = true;
                    OnPropertyChanged();
                    _suppressEvents = false;
                }
            }
        }
    }

    public string CurrentUserExplanation =>
        $"{Environment.UserName}";

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

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void CopyEncryptedText()
    {
        Clipboard.SetText(EncryptedText);
    }

    public void GenerateSecret()
    {
        PlainText = Guid.NewGuid().ToString("N");
    }
}