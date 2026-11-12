# Spikes

Questions about Avalonia 12.1.4 that decide how Cornflower is built. Each answer is held by tests in
[`tests/Cornflower.Tests/Spikes/`](../../tests/Cornflower.Tests/Spikes/), which assert what Avalonia does today, so an Avalonia
upgrade that changes the answer turns them red. All of it is headless, with Skia, on Windows and on Linux CI. None of it has been
tried in a real window or on a device.

## 1. Swapping the theme at run time

**Question.** With two `Styles`, each holding a `ControlTheme` for `Button` with its own template and its own value for one brush
key, what happens when the first is replaced by the second in `Application.Styles`, or in the window's `Styles`?

**Answer.** A control picks its `ControlTheme` once, when it joins a logical tree. Replacing the `Styles` doesn't re-template a
control that's already there, in either place. A `DynamicResource` brush follows at once, without the dispatcher running, including
one set by the old theme's own setter. Controls added afterwards get the new theme. The nudge is to take the content out of the tree
and put it back (`window.Content = null; window.Content = root;`), which re-templates it with the new theme. The same rule bites the
other way: `Styles` added to a window after its content is set don't theme that content, even before the window is shown.

**Evidence.** `ThemeSwapTests`: `Replacing_The_Era_Does_Not_Retemplate_A_Shown_Control`,
`Replacing_The_Era_Moves_DynamicResource_Brushes_At_Once`, `Replacing_The_Era_Themes_Controls_Added_Afterwards`,
`Detaching_And_Reattaching_The_Content_Retemplates_With_The_New_Era` and `Styles_Added_After_The_Content_Do_Not_Theme_It`, each run
with the styles in the application and in the window.

**Consequence.** The era is a start-up choice. A live switch would mean re-hosting the content, which rebuilds every template. What
that costs, and what state in template parts survives it, hasn't been measured. Colours that differ between light and dark, or between accents, can still
change live if they are `DynamicResource`s.

## 2. A control with no `ControlTheme`

**Question.** With no theme installed, what do `Button`, `TextBox` and `Window` do? With `SimpleTheme` underneath and our own theme
for `Button` only, which wins?

**Answer.** `ContentControl`'s `Template` property has a default value, a bare `ContentPresenter` named `PART_ContentPresenter`. So
`Button`, `Window` and other content controls still show their content, unstyled: the button's text is measured and the window lays
its content out at full size. `TextBox` has no template: no visual children and a desired size of 0 × 0. Nothing throws, and nothing
is logged at `Warning` or above. Over `SimpleTheme`, our `Button` theme wins and `TextBox` and `Window` get Simple's templates. Order
matters: the later `Styles` wins, so our theme put in front of Simple loses to it.

**Evidence.** `MissingThemeTests`: `With_No_Theme_A_Button_Gets_A_Bare_ContentPresenter`,
`With_No_Theme_A_Window_Still_Presents_Its_Content`, `With_No_Theme_A_TextBox_Has_No_Template_And_No_Height`,
`With_No_Theme_Nothing_Throws_Or_Logs_A_Warning`, `Over_Simple_Our_Button_Theme_Wins_And_Other_Controls_Fall_Through` and
`Under_Simple_Our_Button_Theme_Loses`.

**Consequence.** A missing theme fails silently, and for content controls it looks half right, so a test can't rely on a log or an
exception to catch one. `Wp81Theme` goes after `SimpleTheme` in `Application.Styles`.

## 3. Subclass themes

**Question.** For a subclass of `ComboBox`: (a) does a theme `BasedOn="{StaticResource {x:Type ComboBox}}"` get the base's template
and add setters? (b) does `StyleKeyOverride` returning `typeof(ComboBox)` get the base's theme? (c) what does a subclass with
neither get?

**Answer.** (a) Yes, with the subclass's theme in a separate file from the base's, both merged into one `Styles`' resources as
`Wp81Theme` merges its control files. It builds with no warning, and at run time gets the base's template and setters and its own
setter. (b) Yes, unchanged. (c) Nothing: theme lookup is by the exact `StyleKey` and doesn't walk base types. A `ComboBox` subclass
gets no template at all. A `Button` subclass gets `ContentControl`'s bare fallback, not the base's template, even with `SimpleTheme`
installed. Neither logs a warning.

**Evidence.** `SubclassThemeTests`: `A_BasedOn_Theme_Gets_The_Base_Template_And_Setters_And_Adds_Its_Own`,
`StyleKeyOverride_To_The_Base_Type_Gets_The_Base_Theme_Unchanged`, `A_Subclass_With_No_Theme_And_No_StyleKeyOverride_Gets_Nothing`
and `A_Button_Subclass_With_No_Theme_Gets_The_Bare_Fallback_Not_Simples_Button`.

**Consequence.** A subclass that keeps the WinRT name works either way: `StyleKeyOverride` if it looks like its base, or its own
theme `BasedOn` the base's in a file of its own. A subclass with neither is a silent bug, so every Cornflower subclass needs a test
that it's templated.

## 4. The tilt's press point

**Question.** A behaviour on a stock `Button` subscribes to `PointerPressedEvent` on tunnel and bubble with `handledEventsToo`.
`Button` marks the press handled. Driven by Avalonia.Headless mouse input, does the handler fire, and does `e.GetPosition(button)` give
the press point? What happens on release, when the pointer leaves, and when capture is lost? Is tunnel alone enough?

**Answer.** The press arrives twice: on the tunnel route unhandled, then on the bubble route handled. Without `handledEventsToo`,
only the tunnel sees it. `e.GetPosition(button)` is the press point in the button's own space. The pointer is captured by the template
part that was hit (Simple's `ContentPresenter`), not by the button. Leaving the button raises `PointerExited` but keeps capture, so a
release outside the button still reaches it, with its position in the button's space. `PointerCaptureLost` is raised directly on the
captured element and on each of its ancestors, so a handler on the button sees it, after every release and when capture is taken
away mid-press. Taking capture away also clears `IsPressed`. Tunnel alone, even without `handledEventsToo`, sees the press, the
release and the capture loss on a stock `Button`. Avalonia.Headless touch input behaves the same.

**Evidence.** `TiltPressPointTests`: `The_Press_Reaches_Both_Routes_With_HandledEventsToo`,
`Without_HandledEventsToo_The_Bubble_Misses_The_Press`, `GetPosition_Gives_The_Press_Point_In_The_Buttons_Space`,
`The_Pointer_Is_Captured_By_The_Template_Part_That_Was_Hit_Not_The_Button`, `Leaving_And_Releasing_Outside_Still_Releases_Then_Loses_Capture`,
`Capture_Taken_Mid_Press_Raises_CaptureLost_On_The_Button`, `Tunnel_Alone_Without_HandledEventsToo_Sees_Press_Release_And_Capture_Lost`
and `Headless_Touch_Behaves_Like_The_Mouse`.

**Consequence.** The tilt can be an attached behaviour on stock controls. It takes the press point from the tunnel, and resets on
`PointerReleased` or `PointerCaptureLost` on the control itself. Keeping `handledEventsToo` does no harm, and would also cover an
ancestor that handles the tunnel, which isn't tested. Real touch hardware is untested.

## 5. The unit of `TextBlock.LetterSpacing`

**Question.** Is `LetterSpacing` in DIPs or, like WinRT's `CharacterSpacing`, in thousandths of an em?

**Answer.** DIPs, whatever the font size. Ten characters of Inter at 20 and at 40 grow by exactly 10 × the spacing, so it's added
after every character, the last one included.

**Evidence.** `LetterSpacingUnitTests.LetterSpacing_Adds_That_Many_DIPs_After_Every_Character`, with Inter from
`Avalonia.Fonts.Inter`.

**Consequence.** WinRT's `CharacterSpacing="n"` is `LetterSpacing = n × FontSize / 1000`, so a token for it has to be scaled by the
font size. The trailing space after the last character also widens the measured text, which WinRT may not do. That hasn't been
compared with the reference.

## 6. A theme's values, unset when it goes

**Question.** When a `ControlTheme` goes (a new `Theme`, or none), and a handler of one of the changes takes a value
of another property away, does every property the theme set still go back?

**Answer.** Not always. The value store unsets the values left without a source one property at a time, from the
property registered last to the first, and stops when a handler leaves it fewer values than the index it is at. So a
handler of the last such property's change that takes another value away stops the rest being unset: they keep the
old theme's values, with no change raised, and a new theme that sets the same values raises none either. A value of
a property registered after them, which the theme doesn't set, keeps it from happening.

**Evidence.** `UnsetPassTests`: a theme's two values on a `Border`, the last one's handler taking a third value away;
with nothing after them the first keeps its value through the swap and raises no change, and with a value of a later
property it goes to its default and back.

**Consequence.** `PhoneTextLine` takes its values back when the properties it reads change (`CharacterSpacing` going
takes the `LetterSpacing` it set), and its own properties are registered late, so it would be such a handler. It
doesn't act inside a change at `Unset` or `Inherited` priority, which is what the store raises while it unsets: it
works its values out from the dispatcher instead, at `Normal` priority, ahead of layout. Acting inside the change, with
its bookkeeping beside the `TextBlock`, a swap to a `ControlTheme` based on a text style kept `PhoneTextLine`'s
`Padding` over the derived theme's (`TextBlockTests.A_Value_A_Derived_ControlTheme_Sets_Wins_Over_The_Styles` and
`A_Theme_Setting_MaxHeight_And_BaselineToBaseline_Goes_Cleanly` go red). An attached property of its own, registered
after the three, had kept that working only by being the value after them.

## 7. Where the phone's line can be applied

**Question.** `PhoneTextLine` writes the phone's line (WinRT's `BaselineToBaseline`, its `MaxHeight` line and
`CharacterSpacing`) as `Padding`, `LineHeight` and `LetterSpacing` at style priority, from property-changed handlers, and
so has two documented gaps (#77, the TextBlock README): a value worked out again passes through the value below, and a
value that goes is taken back from the dispatcher. Can the line be applied while the `TextBlock` is laid out, or worked
out by the value store, instead (#81)?

**Answer.** Not for the type ramp's `TextBlock`s, on 12.1.4. Each option, and what stops it:

- **Options 1 and 3**, a `TextBlock` subclass through `StyleKeyOverride`, and its own `TextLayout` through
  `CreateTextLayout`, which only a subclass can override. Another assembly can override `MeasureOverride`,
  `ArrangeOverride`, `CreateTextLayout` and `RenderTextLayout`, but not `RenderCore` (private protected) or `Render`
  (sealed), and `TextLayout`'s height and drawing aren't virtual, so the first baseline's offset would be four
  overrides kept in step. A layout of its own can't reach `TextBlock`'s text run cache (private) or its inlines test
  (internal). What it draws isn't in the properties the type ramp's tests, an app and an extension pack read. And it is
  ladder rung 4: every `TextBlock` an app writes would change type.
- **A presenter's text.** A `ContentPresenter` makes its text from the tree's data templates first, its own before an
  ancestor's, and only with none falls back to Avalonia's: an `AccessText` when it recognises access keys, a
  `TextBlock` when not. So a theme's own template on its presenter could make a text of the theme's (a part, rung 3,
  which apps never write), but it would beat an app's template for the same data, where the Button's plan has an app's
  templates win; and that text would still be the subclass above. It isn't done.
- **Option 2**, an attached layout hook. `Layoutable` declares two events, `LayoutUpdated` and
  `EffectiveViewportChanged`, both raised after a pass, so an attached behaviour has nothing to hook before a
  `TextBlock` is measured, and a value written in `LayoutUpdated` lays it out a second time. **2b**, a part around the
  text: a parent's own measure can set its child's values in the same pass, so a `Decorator` the theme's template puts
  around its presenter could apply the line in layout. Only text inside a template of the theme's has such a parent
  (not the type ramp's, which an app places), and it still writes the three properties, so the value-store gaps stay.
- **Option 4, coercion** (`OverrideMetadata` on `TextBlock`'s three properties, deciding by `GetDiagnostic`'s
  priority). A coercion made while a value comes in sees the priority before it, and when styles are applied again the
  store resets every priority first, so each coercion sees no value set; a coercion asked for again afterwards sees the
  real one. Where the blind answer is the value already there, no change is raised, so nothing hears that it must ask
  again. And a coerced default is a value in the store: when the coercion gives the default back it stays (`IsSet`, and
  `ClearValue` doesn't take it out), and on an inherited property (`LineHeight`, `LetterSpacing`) it stops the parent's
  next value being inherited. Tried on `PhoneTextLine` itself, its handlers writing nothing and asking for the coercions
  again from the dispatcher: 700 of 705 tests pass. `IsSet` stays true where `TextBlockTests` lines 573, 600 and 721
  expect it gone, an app's untriggered `Style` added later loses (line 737), and the test of the posted pass's
  bookkeeping (line 535) has nothing to read. Asking again after every coercion that put the phone's value over another
  fixes line 737 but moves lines 441 (both cases), 481 and 641, as `SetCurrentValue` does (below). Either way it is still
  a pass from the dispatcher, so the values are stale until it runs, as now.
- **Option 4, a binding in each style.** `BindingBase`'s one member is internal, so the only bindings are Avalonia's
  own, and an observable made into a binding gives every target the same value. A binding to the `TextBlock` itself
  with a converter is per target but worked out once, so it doesn't follow a later `FontSize`; one with a path follows
  that one property, so the rest is the converter's fixed parameter, not a `DynamicResource` hook. A `MultiBinding` of
  the `TextBlock`'s own properties works, but #69 measured it at 66.6 and 85.6 µs per `TextBlock` against the handler's
  35.3 and 43.8 (docs/controls/TextBlock/PLAN.md), and a style's setters reach only the `TextBlock`s that have the
  style, not `PhoneContentLine`'s text or the attached properties set on any other `TextBlock`.

**Evidence.** `TextLineHookTests`, on plain Avalonia (`SpikeWindow`):
`Only_A_Subclass_Can_Override_What_A_TextBlocks_Layout_Reads`,
`A_Subclass_Building_Its_Own_Layout_Has_No_Way_To_The_Run_Cache_Or_The_Inlines_Test`,
`A_Subclasss_Own_Spacing_Is_In_The_Layout_It_Draws`, `A_Presenter_Makes_Its_Text_From_The_Trees_Data_Templates_First`,
`The_Layout_Events_Come_After_The_Pass_And_A_Value_Set_There_Lays_Out_Again`,
`A_Parents_Measure_Can_Set_Its_Childs_Values_In_The_Same_Pass`,
`A_Coercion_Sees_The_Priority_Before_The_Value_And_A_Later_One_Sees_It_After`,
`In_A_Reevaluation_Every_Coercion_Is_Blind_And_An_Unchanged_Answer_Raises_Nothing`,
`A_Coerced_Default_Stays_In_The_Store_And_Stops_Inheritance`,
`A_Binding_Of_Ones_Own_Cant_Be_Made_And_An_Observable_Gives_Every_Target_The_Same_Value` and
`A_Binding_To_Itself_Is_Per_Target_But_Doesnt_Follow_Its_Font`. The coercion probes are on properties of their own, so
the test process's `TextBlock` metadata isn't changed; the attempt on `PhoneTextLine` was run and reverted (#81 has it).

**Consequence.** `PhoneTextLine` keeps writing the three properties from handlers, and both gaps stay documented. One
change inside that design was tried and not made: setting the values with `SetCurrentValue`, told apart from an equal
value by `GetDiagnostic` (priority `Unset`, an overridden current value), makes a recompute one change (`LetterSpacing`
−0.048 to −0.09 once, where today it passes through 0), but it doesn't remove the deferred pass, since taking a value
back still removes it inside the store's unsetting, and it makes a `ControlTheme` set after the phone's value win at
once rather than at the next recompute, which `TextBlockTests` lines 441 (both cases), 481 and 641 hold the other way
(#81).

## 8. Telling the keyboard a box is a password

**Question.** A masked box should tell the platform keyboard it holds a password (#104), and every other box keep
what it would have had, an ancestor's `TextInputOptions` included. Can a theme's setter do that, with a binding on
`PasswordChar` that gives no value for an unmasked box?

**Answer.** Not by a setter bound to `PasswordChar`. The three `TextInputOptions` a password needs (`ContentType`,
`IsSensitive`, `ShowSuggestions`) inherit, and a style setter whose binding gives `UnsetValue` still hides an
ancestor's value: the box gets the default. A value set from code at style priority, once taken away, gives the
ancestor's back. Not examined: a nested style whose selector matches masked boxes alone. XAML can't write "any
`PasswordChar`" (` ` can't be written in XML, and a property selector matches one value), and #103's plan already
turned down a selector on a `char` property; a selector built in code is left untried.

**Evidence.** `KeyboardOptionsTests`: the three report `Inherits` and a `Border` takes a parent's `Email`; under a
setter whose binding gives `UnsetValue` it is `Normal`; set at style priority and taken away, `Email` again.

**Consequence.** `PhonePasswordKeyboard`, turned on by the TextBox and PasswordBox themes, sets the three from code
while a box is masked and takes them away entirely while it isn't, as `PhoneTextLine` does its values (and, as it,
from the dispatcher when a value goes, for spike 6). Inheriting, they reach an app's content inside a masked box: a
recorded gap. On the Pixel 8, with it, Gboard shows no suggestions in a masked box, shown or not.
