using System.Drawing;

namespace SecureChat.Common;

/// <summary>
/// 앱 전역 디자인 토큰 (무신사풍 미니멀 흑백 + 포인트 아바타 색).
/// 컨트롤마다 흩어져 중복되던 색·폰트·모서리 반경을 한 곳에서 관리한다.
/// </summary>
public static class Theme
{
    // ── 색상 ─────────────────────────────────────────────────
    public static readonly Color Canvas   = Color.White;                    // 페이지 배경
    public static readonly Color Surface  = Color.FromArgb(245, 245, 245);  // 카드 / hover 배경 (#f5f5f5)
    public static readonly Color Hairline = Color.FromArgb(238, 238, 238);  // 옅은 구분선 (#eeeeee)
    public static readonly Color Border   = Color.FromArgb(221, 221, 221);  // 테두리 (#dddddd)
    public static readonly Color Body     = Color.FromArgb( 34,  34,  34);  // 본문 텍스트 (#222222)
    public static readonly Color InText   = Color.FromArgb( 51,  51,  51);  // 받은 말풍선 텍스트 (#333333)
    public static readonly Color Meta     = Color.FromArgb(153, 153, 153);  // 보조 텍스트 (#999999)
    public static readonly Color Ink      = Color.Black;                    // 강조 / 보낸 말풍선
    public static readonly Color OnInk    = Color.White;                    // 강조 위 텍스트
    public static readonly Color Read     = Color.FromArgb( 41, 182, 246);  // 읽음 표시 (#29B6F6)

    // ── 모서리 반경 토큰 ─────────────────────────────────────
    public const int BubbleRadius = 14;   // 말풍선
    public const int CardRadius   = 10;   // 아바타 / 카드

    // ── 타이포 ───────────────────────────────────────────────
    public const string FontFamily = "맑은 고딕";
    public static Font Font(float size, FontStyle style = FontStyle.Regular)
        => new(FontFamily, size, style);

    // ── 아바타 포인트 색 (시드 해시로 결정) ──────────────────
    public static readonly Color[] AvatarPalette =
    [
        Color.FromArgb( 52, 152, 219),   // blue
        Color.FromArgb(231,  76,  60),   // red
        Color.FromArgb( 46, 204, 113),   // green
        Color.FromArgb(155,  89, 182),   // purple
        Color.FromArgb(230, 126,  34),   // orange
    ];

    public static Color AvatarColor(string seed)
        => AvatarPalette[Math.Abs(seed.GetHashCode()) % AvatarPalette.Length];
}
