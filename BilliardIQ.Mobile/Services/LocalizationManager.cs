using System.ComponentModel;

namespace BilliardIQ.Mobile.Services;

public  sealed partial class LocalizationManager : INotifyPropertyChanged
{
    public static readonly LocalizationManager Instance = new();

    private string _language = Preferences.Default.Get("app_language", "en");

    private static readonly Dictionary<string, Dictionary<string, string>> _translations = new()
    {
        ["en"] = new()
        {
            // Stats card
            ["Stats_Matches"]   = "Matches",
            ["Stats_Average"]   = "Average",
            ["Stats_HighRun"]   = "High Run",
            ["Stats_LastMatch"] = "Last Match",
            ["Result_Win"]      = "Win",
            ["Result_Loss"]     = "Loss",
            ["Result_Draw"]     = "Draw",
            ["Result_NoGame"]   = "-",
            ["Serie"]           = "Serie",

            // Game list
            ["GameList_Delete"] = "Delete",

            // New game
            ["NewGame_Title"]            = "Add New Game",
            ["NewGame_UpdateTitle"]      = "Update Game",
            ["NewGame_OpponentName"]     = "Enter Opponent Name",
            ["NewGame_Location"]         = "Enter Location",
            ["NewGame_Score"]            = "Score",
            ["NewGame_MyScore"]          = "My Score",
            ["NewGame_OpponentScore"]    = "Opponent Score",
            ["NewGame_HighestRun"]       = "Highest Run",
            ["NewGame_Innings"]          = "Innings",
            ["Stats_BestAverage"]        = "Best Avg",
            ["NewGame_SelectBall"]       = "Select Ball",
            ["NewGame_Notes"]            = "Notes (optional)",
            ["NewGame_NotesPlaceholder"] = "Add a description or notes...",
            ["NewGame_ScoreboardPhoto"]  = "Scoreboard Photo",
            ["NewGame_Remove"]           = "Remove",

            // Common actions
            ["Action_Save"]   = "Save",
            ["Action_Update"] = "Update",
            ["Action_Ok"]     = "OK",

            // Profile
            ["Profile_Title"]        = "Player Profile",
            ["Profile_CreateHeader"] = "Create a new Player",
            ["Profile_UpdateHeader"] = "Update Player",
            ["Profile_Name"]         = "Name *",
            ["Profile_Surname"]      = "Surname *",
            ["Profile_Country"]      = "Country *",
            ["Profile_SelectCountry"]= "Select Country",
            ["Profile_City"]         = "City *",
            ["Profile_SelectCity"]   = "Select City",
            ["Profile_Club"]         = "Club (optional)",
            ["Profile_Association"]  = "Association (optional)",
            ["Profile_Email"]        = "Email *",
            ["Profile_Phone"]        = "Phone (optional)",
            ["Profile_Level"]        = "Level",
            ["Profile_SelectLevel"]  = "Select Level",
            ["Profile_Language"]     = "Language",
            ["Profile_Saved"]        = "Player saved",
            ["Search_Placeholder"]   = "Search...",
            ["Search_NoResults"]     = "No results found",
            ["Action_Cancel"]        = "Cancel",
            ["Ocr_DetectedTitle"]    = "Scores Detected",
            ["Ocr_DetectedMsg"]      = "Found {0} – {1}. Apply to score fields?",
            ["Ocr_Yes"]              = "Apply",
            ["Ocr_No"]               = "Skip",
            ["Ocr_Failed"]           = "Could not read scores from photo",
            ["Ocr_Reading"]          = "Reading scoreboard...",

            // New game — profile guard
            ["NewGame_NoProfile_Title"]   = "Profile Required",
            ["NewGame_NoProfile_Message"] = "You need to create a player profile before adding a game.",
            ["NewGame_NoProfile_Ok"]      = "Go to Profile",

            // Game (Play) page
            ["Game_Title"]       = "3-Cushion Billiards",
            ["Game_Description"] = "Tap PLAY to start a 2-player 3-cushion match.",
            ["Game_Play"]        = "PLAY",
            ["Game_Player1"]     = "Player 1",
            ["Game_Player2"]     = "Player 2",

            // Platform not supported
            ["Platform_DoesNotSupport"]         = "Platform Not Supported",
            ["Platform_DoesNotSupport_Message"] = "The 3-cushion game only works on Android.",
            ["Platform_DoesNotSupport_Ok"]      = "OK",
        },
        ["tr"] = new()
        {
            ["Stats_Matches"]   = "Maçlar",
            ["Stats_Average"]   = "Ortalama",
            ["Stats_HighRun"]   = "En Yüksek",
            ["Stats_LastMatch"] = "Son Maç",
            ["Result_Win"]      = "Kazandı",
            ["Result_Loss"]     = "Kaybetti",
            ["Result_Draw"]     = "Berabere",
            ["Result_NoGame"]   = "-",
            ["Serie"]           = "Seri",

            ["GameList_Delete"] = "Sil",

            ["NewGame_Title"]            = "Yeni Maç Ekle",
            ["NewGame_UpdateTitle"]      = "Maçı Güncelle",
            ["NewGame_OpponentName"]     = "Rakip Adı",
            ["NewGame_Location"]         = "Lokasyon",
            ["NewGame_Score"]            = "Skor",
            ["NewGame_MyScore"]          = "Benim Skorum",
            ["NewGame_OpponentScore"]    = "Rakip Skoru",
            ["NewGame_HighestRun"]       = "En Yüksek Seri",
            ["NewGame_Innings"]          = "El Sayısı",
            ["Stats_BestAverage"]        = "En İyi Ort.",
            ["NewGame_SelectBall"]       = "Top Seç",
            ["NewGame_Notes"]            = "Notlar (opsiyonel)",
            ["NewGame_NotesPlaceholder"] = "Açıklama veya not ekle...",
            ["NewGame_ScoreboardPhoto"]  = "Skorbord Fotoğrafı",
            ["NewGame_Remove"]           = "Kaldır",

            ["Action_Save"]   = "Kaydet",
            ["Action_Update"] = "Güncelle",
            ["Action_Ok"]     = "Tamam",

            ["Profile_Title"]         = "Oyuncu Profili",
            ["Profile_CreateHeader"]  = "Yeni Oyuncu Oluştur",
            ["Profile_UpdateHeader"]  = "Oyuncuyu Güncelle",
            ["Profile_Name"]          = "Ad *",
            ["Profile_Surname"]       = "Soyad *",
            ["Profile_Country"]       = "Ülke *",
            ["Profile_SelectCountry"] = "Ülke Seç",
            ["Profile_City"]          = "Şehir *",
            ["Profile_SelectCity"]    = "Şehir Seç",
            ["Profile_Club"]          = "Kulüp (opsiyonel)",
            ["Profile_Association"]   = "Temsilcilik (opsiyonel)",
            ["Profile_Email"]         = "E-posta *",
            ["Profile_Phone"]         = "Telefon (opsiyonel)",
            ["Profile_Level"]         = "Seviye",
            ["Profile_SelectLevel"]   = "Seviye Seç",
            ["Profile_Language"]      = "Dil",
            ["Profile_Saved"]         = "Oyuncu kaydedildi",
            ["Search_Placeholder"]    = "Ara...",
            ["Search_NoResults"]      = "Sonuç bulunamadı",
            ["Action_Cancel"]         = "İptal",
            ["Ocr_DetectedTitle"]     = "Skor Algılandı",
            ["Ocr_DetectedMsg"]       = "{0} – {1} skoru bulundu. Skor alanlarına uygulansın mı?",
            ["Ocr_Yes"]               = "Uygula",
            ["Ocr_No"]                = "Atla",
            ["Ocr_Failed"]            = "Fotoğraftan skor okunamadı",
            ["Ocr_Reading"]           = "Skorbord okunuyor...",

            // New game — profile guard
            ["NewGame_NoProfile_Title"]   = "Profil Gerekli",
            ["NewGame_NoProfile_Message"] = "Maç ekleyebilmek için önce oyuncu profili oluşturmanız gerekiyor.",
            ["NewGame_NoProfile_Ok"]      = "Profile Git",

            // Game (Play) page
            ["Game_Title"]       = "3-Bant Bilardo",
            ["Game_Description"] = "2 oyunculu 3-bant maçı başlatmak için OYNA'ya dokun.",
            ["Game_Play"]        = "OYNA",
            ["Game_Player1"]     = "Oyuncu 1",
            ["Game_Player2"]     = "Oyuncu 2",

            // Platform not supported
            ["Platform_DoesNotSupport"]         = "Platform Desteklenmiyor",
            ["Platform_DoesNotSupport_Message"] = "3-Bant oyunu yalnızca Android'de çalışır.",
            ["Platform_DoesNotSupport_Ok"]      = "Tamam",
        }
    };
    public string CurrentLanguage => _language;
    public string this[string key]
    {
        get
        {
            if (_translations.TryGetValue(_language, out var dict) && dict.TryGetValue(key, out var val))
                return val;
            if (_translations["en"].TryGetValue(key, out var fallback))
                return fallback;
            return key;
        }
    }

    public void SetLanguage(string language)
    {
        if (_language == language) return;
        _language = language;
        Preferences.Default.Set("app_language", language);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private LocalizationManager() { }
}
