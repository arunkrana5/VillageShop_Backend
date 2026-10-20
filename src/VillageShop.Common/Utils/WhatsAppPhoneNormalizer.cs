using System;
using System.Text.RegularExpressions;

namespace VillageShop.Common.Utils;

public class NormalizedPhoneResult
{
    public bool IsValid { get; set; }
    public string NormalizedPhone { get; set; } = string.Empty;
    public string ChatId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public static class WhatsAppPhoneNormalizer
{
    /// <summary>
    /// Normalizes any input phone number (raw, formatted, with/without +91, with/without 0)
    /// into the exact format required by Green API.
    /// Green API requires {countryCode}{subscriberNumber}@c.us without '+' or leading '0'.
    /// For India, that is 91XXXXXXXXXX (12 digits total).
    /// </summary>
    public static NormalizedPhoneResult Normalize(string? rawPhone)
    {
        var result = new NormalizedPhoneResult();

        if (string.IsNullOrWhiteSpace(rawPhone))
        {
            result.IsValid = false;
            result.ErrorMessage = "Phone number cannot be empty.";
            return result;
        }

        string trimmed = rawPhone.Trim();
        bool hasPlus = trimmed.StartsWith("+");
        string digits = Regex.Replace(trimmed, @"\D", "");

        if (string.IsNullOrEmpty(digits))
        {
            result.IsValid = false;
            result.ErrorMessage = "Phone number contains no valid digits.";
            return result;
        }

        // Check for dummy repeating digits (e.g. 0000000000, 1111111111)
        if (Regex.IsMatch(digits, @"^(\d)\1{9,}$"))
        {
            result.IsValid = false;
            result.ErrorMessage = "Invalid phone number containing repeating digits.";
            return result;
        }

        // 1. Remove leading trunk prefix 0 if 11 digits (e.g. 08581896270 -> 8581896270)
        if (digits.Length == 11 && digits.StartsWith("0"))
        {
            digits = digits.Substring(1);
        }

        // 2. Remove leading 0 after country code 91 if 13 digits (e.g. +91 08581896270 -> 918581896270)
        if (digits.Length == 13 && digits.StartsWith("910"))
        {
            digits = "91" + digits.Substring(3);
        }

        // 3. Remove duplicate country code if 14 digits starting with 9191 (e.g. 91918581896270 -> 918581896270)
        if (digits.Length == 14 && digits.StartsWith("9191"))
        {
            digits = digits.Substring(2);
        }

        // 4. Standard 10-digit Indian mobile number (starts with 6, 7, 8, 9, including 91xxxxxxxx)
        if (digits.Length == 10)
        {
            if (Regex.IsMatch(digits, @"^[6-9]\d{9}$"))
            {
                string norm = "91" + digits;
                result.IsValid = true;
                result.NormalizedPhone = norm;
                result.ChatId = norm + "@c.us";
                return result;
            }
            else
            {
                result.IsValid = false;
                result.ErrorMessage = "Indian mobile numbers must be 10 digits starting with 6, 7, 8, or 9.";
                return result;
            }
        }

        // 5. 12-digit Indian mobile number with 91 prefix (91 + 10 digits starting with 6, 7, 8, or 9)
        if (digits.Length == 12 && digits.StartsWith("91"))
        {
            string national = digits.Substring(2);
            if (Regex.IsMatch(national, @"^[6-9]\d{9}$"))
            {
                result.IsValid = true;
                result.NormalizedPhone = digits;
                result.ChatId = digits + "@c.us";
                return result;
            }
            else
            {
                result.IsValid = false;
                result.ErrorMessage = "Invalid 10-digit Indian subscriber number following country code 91.";
                return result;
            }
        }

        // 6. International numbers with explicit '+' prefix or valid ITU E.164 length (10 to 15 digits)
        if (hasPlus && digits.Length >= 10 && digits.Length <= 15)
        {
            result.IsValid = true;
            result.NormalizedPhone = digits;
            result.ChatId = digits + "@c.us";
            return result;
        }

        result.IsValid = false;
        result.ErrorMessage = $"Invalid phone number ({digits.Length} digits). Please provide a valid 10-digit Indian mobile number (e.g. 9876543210) or international number with country code.";
        return result;
    }
}
