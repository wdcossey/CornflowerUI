# PasswordBox: plan

[#37](https://github.com/wdcossey/CornflowerUI/issues/37). The 8.1 PasswordBox. Avalonia has no PasswordBox: its
`TextBox` masks its text with `PasswordChar` and shows it with `RevealPassword`. So the phone's PasswordBox is a
`ControlTheme` for `TextBox`, keyed `PasswordBox`, WinRT's name: the phone's style and template, the TextBox's (#36) but
for a third row, the "Show Password" CheckBox.

## Where the values come from

Every value below is read from the 8.1 Update 1 runtime's default style for `PasswordBox`,
`R:\cornflower-reference\flash-update1\xbf-json\passwordbox.json` (and its XAML beside it), and its keys' values from
`themeresources.json`. They equal the SDK's design files ([CLAUDE.md](../../../CLAUDE.md), "Reference material").

The captures **check** the theme. They are the reference app's, on the reference profile (device 11, 1080 × 1920 at
2.4), committed in [`ref/wp81/PasswordBox/`](../../../ref/wp81/PasswordBox/README.md):

- run `20261108-1443-dev11`: `password`, `new PasswordBox { Header = "password box", Password = "secret",
  IsPasswordRevealButtonEnabled = true }`, in every state of both groups, dark and light;
- run `20261112-1429-dev11`, made for this plan from a copy of `tools/refapp`: `empty` (a placeholder, no reveal
  button), `no-header`, `reveal-no-header` and `reveal-empty` (the reveal button on an empty box). No earlier run had a
  PasswordBox without the reveal button, or with it and no password. A fifth specimen, ticking the RevealButton once
  loaded, was unticked by the template's `ButtonVisible` as the box loaded: there is no capture of a shown password.

**What no capture has**: a shown password, a caret, a selection, the keyboard. The forced `Disabled` state leaves the
box's parts enabled (the RevealButton keeps its enabled look), where a disabled box disables them (below).

## The phone's style

`passwordbox.json`: one unkeyed style for `PasswordBox`, 16 setters and a template. The setters are the TextBox's
(#36's plan), less `TextWrapping` and `ScrollViewer.IsDeferredScrollingEnabled` (a PasswordBox has no wrapping).
`PasswordChar` isn't set by the style: WinRT's default is `●` (U+25CF), as the PNGs draw it.

### The template

The TextBox's (#36), but:

```
Grid                                   rows Auto, *, Auto (a third)
  ...BorderElement, HeaderContentPresenter as the TextBox's
  ScrollViewer "ContentElement"        Margin RichEditBoxTextThemeMargin (2.5 all round), not BorderThickness: the same 2.5
  ContentControl "PlaceholderTextContentPresenter"
                                       Margin RichEditBoxTextThemeMargin; no FontSize (the TextBox's sets ContentControlFontSize)
  CheckBox "RevealButton"              Row 2; Foreground TextBoxForegroundHeaderThemeBrush (PhoneForegroundColor); IsTabStop False;
                                       VerticalAlignment Top; HorizontalAlignment Left; Collapsed; Margin PasswordBoxCheckBoxThemeMargin (0,-6.5,0,0)
```

The RevealButton's text, "Show Password", is set by WinRT's code from its resources (the dump's `TextBlock`); the
CheckBox is the phone's own CheckBox style (the dump's parts: `CheckBackground`, `NormalRectangle`, `CheckGlyph`,
`ContentPresenter`, 167.9167 × 57.5), so Wp81's CheckBox theme (#31, PR #92) draws it.

### The visual states

`CommonStates`: `Normal`, `Focused` and `Disabled`, the TextBox's, and **`PointerOver`, empty**: going to it ends
`Normal`'s 0.8, so the box is drawn whole (the dump: no opacity; the PNG: 255 inside, dark). `ButtonStates`:
`ButtonVisible` shows the RevealButton and unticks it; `ButtonCollapsed` hides it and unticks it. From the dumps,
WinRT's code goes to `ButtonVisible` whenever `IsPasswordRevealButtonEnabled`, a password or not (`reveal-empty`), and
`ButtonCollapsed` otherwise (`empty`, `no-header`).

### The layout with the RevealButton

The dump: the box 108.75 tall; `BorderElement` 26.6667 down but only **31.25** tall; `ContentElement` still at
29.1667, 34.1667 tall, past the border's bottom by 2.9; the CheckBox 51.25 down. With no header: 78.75, the border
27.9167, the CheckBox 21.25 down. That is the stage: the reference app's leaves the box 109 (79 with no header) of
height, the header and the CheckBox's row (57.5 less 6.5) take theirs, and the Grid gives its star row what is left.
Avalonia's Grid does the same: on the same stages ours is 108.75 and 78.75, with the border 31.25 and 27.9167, the
dumps'. With room (a `StackPanel`), the border is the TextBox's 39.1667 on both.

The phone's `ContentElement` keeps its top when its row is too short, overflowing below; Avalonia would centre it
(1.5 higher), so the theme puts the content at the top of its row.

## How an app asks for it

Avalonia has no PasswordBox type, so there are three ways an app could ask for the phone's:

| Way | For | Against | |
|---|---|---|---|
| A `ControlTheme` for `TextBox` keyed `PasswordBox`: `<TextBox Theme="{DynamicResource PasswordBox}" />` | WinRT's name, as the 16 text styles are keyed by theirs; a port is a search and replace (`<PasswordBox` to `<TextBox Theme=...`); the phone's PasswordBox is its own style, not the TextBox's, and this is; an explicit theme is never reached by `HoleParts` or another theme by type; the TextBox's template stays two rows | The app writes the key | **Chosen** |
| The TextBox theme showing the RevealButton whenever `PasswordChar` is set | No key | A third row and a CheckBox in every TextBox's template; a selector on a `char` property; the TextBox theme (#36's file) changed; a masked TextBox that isn't a PasswordBox gets one | |
| A `PasswordBox` subclass in `Cornflower.Controls` (rung 4 or 5) | The WinRT type and `Password` | Rung 4 is for a changed input path, and nothing here changes one; every base member keeps its meaning, so a theme reaches it all (rung 1) | |

A TextBox with `PasswordChar` and no key is already the phone's PasswordBox without its reveal button, WinRT's
default: the same parts, sizes and pixels at rest, focused and disabled (the `empty` and `no-header` captures), all but
the pointer-over look and the placeholder's size at another `FontSize`. So the key
is needed only for the reveal button, but it is the way an app asks for a PasswordBox, with `PasswordChar` `●` and one
line.

**The reveal button is the `revealPasswordButton` class**, WinRT's `IsPasswordRevealButtonEnabled`. It is Avalonia's
own way to ask a TextBox for one: Simple's and Fluent's themes show an eye button for it. Under the `PasswordBox` key,
the same class shows the phone's "Show Password" CheckBox instead. An attached property would be new API for what
Avalonia already says with a class (rung 1, not 2).

**The RevealButton is Avalonia's `RevealPassword`, both ways**: ticking it shows the password; an app setting
`RevealPassword` ticks it. Wp81's CheckBox theme draws it (by type, as a template's part; `HoleParts` leaves it, as the
template is Wp81's); its foreground is the template's key, set in the PasswordBox theme's style as the CheckBox theme's
own setter would win over the template's value.

## Avalonia's TextBox against the phone's PasswordBox

As the TextBox's (#36's plan), and:

| Difference | The phone | Avalonia | Rung |
|---|---|---|---|
| The control | `PasswordBox` | `TextBox` with `PasswordChar` | 1, a keyed theme (above) |
| `Password`, `PasswordChanged` | the PasswordBox's | `Text`, `TextChanged` | none: Avalonia's |
| `PasswordChar` | `●` by default | `\0` (none) by default | 1, the key's style sets `●` |
| `IsPasswordRevealButtonEnabled` | a property | the `revealPasswordButton` class | 1 |
| The RevealButton | a CheckBox, "Show Password", in a third row | Simple's eye button inside the box | 1, the phone's CheckBox, `IsChecked` to `RevealPassword` |
| Unticked when it shows or hides (`ButtonStates`) | WinRT's states set `IsChecked` False | no states | 2, `PhoneRevealButton` (new, internal to the theme): the CheckBox showing or hiding clears `RevealPassword`, so taking the class away never leaves a password shown (the first review's finding) |
| The RevealButton takes no focus | `IsTabStop` False: never focused, so a tap leaves the box focused and its keyboard up | `IsTabStop` keeps it out of the Tab order only; a press focuses it | 1, `Focusable` False too (the first review's finding: a tap took the box's focus) |
| Read only | a PasswordBox can't be | `IsReadOnly` | 1, no reveal button on a read-only box, as Simple's and Fluent's gate the class |
| The CheckBox over the box's bottom | lifted 6.5 by `PasswordBoxCheckBoxThemeMargin`, later in the tree, so its transparent top takes a press there | the same | none: the phone's. A press in the box's bottom 6.67 ticks it, on the phone too |
| Disabled | the parts disabled too (WinRT's `IsEnabled` reaches them; the forced capture can't show it) | the same | none: the CheckBox's own Disabled look |
| `PointerOver` | ends Normal's 0.8 | `:pointerover`, a mouse's | 1, the box drawn whole under a pointer |
| The placeholder's size | the box's | | 1, no `FontSize` on the part |
| One line | a PasswordBox has no wrapping or return | `TextWrapping`, `AcceptsReturn` | 1, the key's style sets `NoWrap` and `False` |

## Behaviours

Each has a test in `tests/Cornflower.Tests/Controls/PasswordBoxTests.cs` unless named.

1. **The key** gives a TextBox the phone's PasswordBox template; a TextBox without it keeps the TextBox's.
   `The_Key_Gives_A_TextBox_The_Phones_PasswordBox`.
2. **The style's values**, dark and light: the TextBox's, `PasswordChar` `●`, one line, the header's.
   `The_Style_Sets_What_The_Phone_Read`.
3. **Without the reveal button**, the TextBox's sizes: 65.8333, 39.1667 with no header, the text at 9.5833, 30.
   `Without_The_Reveal_Button_It_Is_The_TextBoxs_Size`.
4. **With it**, the phone's layout on its stage, with a password or none: the box 108.75, the border 31.25, the text at
   the top of its row, the CheckBox 51.25 down at the left. `The_Reveal_Button_Is_Below_The_Box_As_On_The_Phone`; and
   with no header 78.75, and with room the full border.
   `The_Border_Takes_What_The_Header_And_The_Reveal_Button_Leave`.
5. **The RevealButton** is Wp81's CheckBox, "Show Password", white (black light), not a tab stop and never focused,
   unticked; ticking it reveals the password and `RevealPassword` ticks it; a tap ticks it and leaves the box focused;
   its foreground is the template's key; hiding it (the class gone, or read only) hides the password, and it shows
   again unticked. `The_Reveal_Button_Is_The_Phones_CheckBox_And_Reveals_The_Password`, `A_Tap_On_Show_Password_Shows_It`,
   `The_Reveal_Buttons_Foreground_Is_The_Templates_Key`, `Hiding_The_Reveal_Button_Hides_The_Password`.
6. **Typing is masked.** `Typing_Is_Masked`.
7. **The pixels** at rest, focused and disabled, the border ending on the phone's row with the reveal button.
   `The_Box_Is_Drawn_As_The_Phones`; **under a pointer** drawn whole. `The_Pointer_Over_It_Draws_The_Box_Whole`.
8. **The placeholder** at the box's size, in the phone's line, only while empty. `The_Placeholder_Is_The_Boxs_Size`.
9. **Disabled**, the CheckBox too. `Disabled_The_Reveal_Button_Is_Too`.
10. **The box's text values** stay out of the header, the placeholder and the CheckBox's text.
    `The_Boxs_Text_Values_Stay_In_The_Text`.
11. **The TextBox's pieces**: its ScrollViewer part's theme; the content inside `RichEditBoxTextThemeMargin`, not the
    box's `BorderThickness`.
    `The_ScrollViewer_Part_Is_The_TextBoxs`.
12. **No global rule**: `StyleBudgetTests`, unchanged. No hole: Avalonia has no PasswordBox type.
13. **The sample's page** (`Sample/PasswordBoxPageTests.cs`): in the catalogue, every box the key's, three with the
    CheckBox, its header from its title, the first box's length following what is typed.

## Decided on the user's behalf

- **A keyed `ControlTheme`, `PasswordBox`**, for `TextBox` (above). To reverse: rename the key; or move the third row
  into the TextBox theme, shown by `PasswordChar`.
- **The reveal button is the `revealPasswordButton` class**, Avalonia's own. To reverse: an attached
  `IsPasswordRevealButtonEnabled`, which the class style would read instead.
- **The text "Show Password" is a resource, `Wp81PasswordBoxRevealText`**, in the theme, so an app can translate it by
  defining the key. WinRT's comes from the system's language. To reverse: a literal on the CheckBox.
- **The `ButtonStates`' unticking is `PhoneRevealButton`**, new and internal: the CheckBox showing or hiding clears
  `RevealPassword`. To reverse: drop its setter; a password shown when the class goes then stays shown.
- **No reveal button on a read-only box**, as Simple's and Fluent's gate the class (WinRT's PasswordBox can't be read
  only). To reverse: the selector without `[IsReadOnly=False]`.
- **`PointerOver` draws the box whole**, as the phone's template does, though a touch never sets `:pointerover` in
  Avalonia 12.1.4 (#93): only a mouse shows it.
- **A disabled box disables its CheckBox**, unlike the forced `Disabled` capture, as WinRT and Avalonia both do for a
  real disabled box.
- **The phone's PasswordBox style is copied whole**, not `BasedOn` the TextBox theme: the phone's is its own style, and
  a `StaticResource` (which `BasedOn` needs) can't reach into another file. The TextBox's ScrollViewer part's theme is
  shared by `DynamicResource`; `PhoneTextBoxLine` and `PhoneHeaderLine` are shared. `TextBox.axaml` is unchanged.
- **A new capture run**, `20261112-1429-dev11`, from a copy of `tools/refapp`.

## Dependencies and captures

- The TextBox theme (#36), the CheckBox theme (#31), `ControlHeader` and `PhoneHeaderLine` (#96): merged. Every
  resource the style reads is in `Tokens/`, but `HeaderContentPresenterStyle` (its values on the presenter).
- **Owed:** a capture of a shown password (it needs a probe with a real tap, as the template unticks a forced one), the
  caret and the keyboard; the device run on the Pixel 8.

## Checklist

Generated on 2026-11-12 from the phone SDK's `Windows.winmd` and design folder, as #36's. The generator matches
Avalonia's control by name, so it treats PasswordBox as a new control (rung 5) and maps by hand nothing; each row's
Action maps it to Avalonia's `TextBox` under the key.

> Generated by `dotnet run --project tools/winrt-api -- checklist PasswordBox <Windows.winmd> <design folder>`, from the Windows Phone 8.1 SDK's `Windows.winmd`, `generic.xaml` and
> `themeresources.xaml`, read as a specification (ADR 0002), and Avalonia 12.1.4. Tick each row, or give a one-line reason it's left out, and fill in
> Action. The Avalonia column is mechanical: a member of the same name, a part Avalonia's control
> declares, a state by the draft mapping rules (docs/ARCHITECTURE.md). Check each.

WinRT `Windows.UI.Xaml.Controls.PasswordBox`; Avalonia none: a new control (rung 5). Mapped by hand to
`Avalonia.Controls.TextBox` with the `PasswordBox` key (rung 1).

### Properties (9)

| ✓ | WinRT | Type | Avalonia | Action |
|---|---|---|---|---|
| [x] | `PasswordChar` | `String` | none | `TextBox.PasswordChar` (a `char`); the key sets `●` |
| [x] | `Password` | `String` | none | `TextBox.Text` |
| [x] | `MaxLength` | `Int32` | none | `TextBox.MaxLength` |
| [x] | `IsPasswordRevealButtonEnabled` | `Boolean` | none | The `revealPasswordButton` class |
| [x] | `SelectionHighlightColor` | `SolidColorBrush` | none | `TextBox.SelectionBrush`, set to the accent |
| [-] | `PreventKeyboardDisplayOnProgrammaticFocus` | `Boolean` | none | Not applicable: the platform shows the keyboard |
| [x] | `PlaceholderText` | `String` | none | `TextBox.PlaceholderText` |
| [x] | `HeaderTemplate` | `DataTemplate` | none | `cf:ControlHeader.HeaderTemplate` |
| [x] | `Header` | `Object` | none | `cf:ControlHeader.Header` |

### Events (3)

| ✓ | WinRT | Handler | Avalonia | Action |
|---|---|---|---|---|
| [x] | `ContextMenuOpening` | `ContextMenuOpeningEventHandler` | none | `ContextRequested`; no context menu set, as the TextBox's |
| [x] | `PasswordChanged` | `RoutedEventHandler` | none | `TextBox.TextChanged` |
| [x] | `Paste` | `TextControlPasteEventHandler` | none | `TextBox.PastingFromClipboard` |

### Methods (1)

| ✓ | WinRT | Avalonia | Action |
|---|---|---|---|
| [x] | `void SelectAll()` | none | `TextBox.SelectAll()` |

### Default style: setters (16)

| ✓ | Property | Value | Avalonia | Action |
|---|---|---|---|---|
| [x] | `MinWidth` | `{ThemeResource TextControlThemeMinWidth}` | none | Set |
| [x] | `MinHeight` | `{ThemeResource TextControlThemeMinHeight}` | none | Set |
| [x] | `SelectionHighlightColor` | `{ThemeResource TextSelectionHighlightColorThemeBrush}` | none | `SelectionBrush` |
| [x] | `Foreground` | `{ThemeResource TextBoxForegroundThemeBrush}` | none | Set |
| [x] | `Background` | `{ThemeResource TextBoxBackgroundThemeBrush}` | none | Set |
| [x] | `BorderBrush` | `{ThemeResource TextBoxBorderThemeBrush}` | none | Set |
| [x] | `BorderThickness` | `{ThemeResource TextControlBorderThemeThickness}` | none | Set |
| [x] | `FontFamily` | `{ThemeResource PhoneFontFamilyNormal}` | none | Set |
| [x] | `FontSize` | `{ThemeResource ContentControlFontSize}` | none | Set |
| [-] | `ScrollViewer.HorizontalScrollMode` | `Auto` | none | No Avalonia member, as the TextBox's |
| [-] | `ScrollViewer.VerticalScrollMode` | `Auto` | none | The same |
| [x] | `ScrollViewer.HorizontalScrollBarVisibility` | `Hidden` | `ScrollViewer.HorizontalScrollBarVisibility (attached)` | Avalonia's TextBox sets it itself (`Hidden` for one line), as #36 found |
| [x] | `ScrollViewer.VerticalScrollBarVisibility` | `Hidden` | `ScrollViewer.VerticalScrollBarVisibility (attached)` | Set |
| [x] | `Padding` | `{ThemeResource TextControlThemePadding}` | none | Set |
| [x] | `Margin` | `{ThemeResource TextControlMarginThemeThickness}` | none | Set |
| [x] | `VerticalAlignment` | `Top` | none | Set |

### Template parts (5)

The mechanical `PART_` names are for a new control's parts; this is Avalonia's TextBox, so its parts are the TextBox
theme's: Avalonia's own names where it looks for one, the phone's otherwise.

| ✓ | Part | Element | Avalonia | Action |
|---|---|---|---|---|
| [x] | `BorderElement` | `Border` | `PART_BorderElement` | `BorderElement`, as the TextBox's |
| [x] | `HeaderContentPresenter` | `ContentPresenter` | `PART_HeaderContentPresenter` | `HeaderContentPresenter`, from `ControlHeader` |
| [x] | `ContentElement` | `ScrollViewer` | `PART_ContentElement` | `PART_ScrollViewer`, Avalonia's, in `RichEditBoxTextThemeMargin` |
| [x] | `PlaceholderTextContentPresenter` | `ContentControl` | `PART_PlaceholderTextContentPresenter` | A `ContentPresenter`, as the TextBox's, at the box's size |
| [x] | `RevealButton` | `CheckBox` | `PART_RevealButton` | `RevealButton`, Wp81's CheckBox, `IsChecked` to `RevealPassword` |

### Visual states (6)

| ✓ | Group | State | What it changes | Avalonia | Action |
|---|---|---|---|---|---|
| [x] | `CommonStates` | `Disabled` | `BorderElement.Background = {ThemeResource TextBoxDisabledBackgroundThemeBrush}`<br>`BorderElement.BorderBrush = {ThemeResource TextBoxDisabledBorderThemeBrush}`<br>`ContentElement.Foreground = {ThemeResource TextBoxDisabledForegroundThemeBrush}`<br>`PlaceholderTextContentPresenter.Foreground = {ThemeResource TextBoxDisabledForegroundThemeBrush}`<br>`HeaderContentPresenter.Foreground = {ThemeResource TextBoxDisabledHeaderForegroundThemeBrush}` | `^:disabled` | Themed |
| [x] | `CommonStates` | `Normal` | `BorderElement.Opacity: DoubleAnimation Duration=0 To={ThemeResource TextControlBorderThemeOpacity}` | the theme's plain setters | The Border's 0.8 |
| [x] | `CommonStates` | `PointerOver` | nothing | `^:pointerover` | Opacity 1: Normal's 0.8 ends |
| [x] | `CommonStates` | `Focused` | `BorderElement.BorderBrush = {ThemeResource TextSelectionHighlightColorThemeBrush}`<br>`PlaceholderTextContentPresenter.Opacity: DoubleAnimation Duration=0 To=0`<br>`BorderElement.Background = {ThemeResource TextBoxFocusedBackgroundThemeBrush}` | `^:focus-visible` | `^:focus`, as the TextBox's |
| [x] | `ButtonStates` | `ButtonVisible` | `RevealButton.Visibility = Visible`<br>`RevealButton.IsChecked = False` | `^:buttonvisible` | `^.revealPasswordButton[IsReadOnly=False]`; unticked by `PhoneRevealButton` |
| [x] | `ButtonStates` | `ButtonCollapsed` | `RevealButton.Visibility = Collapsed`<br>`RevealButton.IsChecked = False` | `^:buttoncollapsed` | Without the class; unticked the same way |

### Transitions (0)

_none_

### Resources (23)

| ✓ | Key | Dark | Light | In Cornflower's Tokens | Action |
|---|---|---|---|---|---|
| [x] | `TextControlThemeMinWidth` | `51` | same | yes | By key |
| [x] | `TextControlThemeMinHeight` | `34` | same | yes | By key, the box's and `ContentRow`'s |
| [x] | `TextSelectionHighlightColorThemeBrush` | `SolidColorBrush {ThemeResource SystemColorControlAccentColor}` | same | yes | By key |
| [x] | `TextBoxForegroundThemeBrush` | `SolidColorBrush {ThemeResource TextBoxForegroundColor} → #FF000000` | same | yes | By key |
| [x] | `TextBoxBackgroundThemeBrush` | `SolidColorBrush {ThemeResource TextBoxBackgroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource TextBoxBackgroundColor} → #FFBFBFBF` | yes | By key |
| [x] | `TextBoxBorderThemeBrush` | `SolidColorBrush {ThemeResource TextBoxBorderColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource TextBoxBorderColor} → #FFBFBFBF` | yes | By key |
| [x] | `TextControlBorderThemeThickness` | `2.5` | same | yes | By key |
| [x] | `PhoneFontFamilyNormal` | `Segoe WP` | same | yes | By key |
| [x] | `ContentControlFontSize` | `20.26` | same | yes | By key |
| [x] | `TextControlThemePadding` | `7,0.8,8,0` | same | yes | By key |
| [x] | `TextControlMarginThemeThickness` | `0,9.5,0,9.5` | same | yes | By key |
| [x] | `TextBoxDisabledBackgroundThemeBrush` | `SolidColorBrush Transparent` | same | yes | By key |
| [x] | `TextBoxDisabledBorderThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | By key |
| [x] | `TextBoxDisabledForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowSolidColor} → #FF747474` | `SolidColorBrush {ThemeResource PhoneBaseLowSolidColor} → #FFBFBFBF` | yes | By key |
| [x] | `TextBoxDisabledHeaderForegroundThemeBrush` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #73FFFFFF` | `SolidColorBrush {ThemeResource PhoneBaseLowColor} → #40000000` | yes | By key |
| [x] | `TextControlBorderThemeOpacity` | `0.8` | same | yes | By key |
| [x] | `TextBoxFocusedBackgroundThemeBrush` | `SolidColorBrush #FFFFFFFF` | same | yes | By key |
| [x] | `HeaderContentPresenterStyle` | `Style TargetType=ContentPresenter` | same | no | Its values on the presenter, as the TextBox's |
| [x] | `TextControlHeaderMarginThemeThickness` | `0,0,0,4.8` | same | yes | By key |
| [x] | `RichEditBoxTextThemeMargin` | `2.5,2.5,2.5,2.5` | same | yes | By key, `ContentRow`'s margin |
| [x] | `TextBoxPlaceholderTextThemeBrush` | `SolidColorBrush {ThemeResource TextBoxPlaceholderColor} → #FF909090` | `SolidColorBrush {ThemeResource TextBoxPlaceholderColor} → #40000000` | yes | By key, through `PlaceholderForeground` |
| [x] | `TextBoxForegroundHeaderThemeBrush` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FFFFFFFF` | `SolidColorBrush {ThemeResource PhoneForegroundColor} → #FF000000` | yes | By key, the RevealButton's foreground |
| [x] | `PasswordBoxCheckBoxThemeMargin` | `0,-6.5,0,0` | same | yes | By key, the RevealButton's margin |
