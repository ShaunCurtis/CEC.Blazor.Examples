# A Simple Bootstrap Modal Dialog for Blazor

## Overview

If a web based SPA [Single Page Application] is going to look like a real application it needs modal dialogs.  This article shows how to build a modal dialog container for Blazor `IComponents` utilising the Bootstrap Modal Dialog CSS Framework.

## Code and Examples

A version of the standard Blazor site implementing modal dialogs is [here at CEC.Blazor.Examples](https://github.com/ShaunCurtis/CEC.Blazor.Examples).

The component is part of my larger Application Framework Library `CEC.Blazor` avaliable on Github at [CEC.Blazor](https://github.com/ShaunCurtis/CEC.Blazor).  

You can see live sites at:

- [CEC.Blazor.Examples Site](https://cec-blazor-examples.azurewebsites.net/)
- [CEC.Blazor WASM Site](https://cec-blazor-wasm.azurewebsites.net/) - look at *Modal Weather*.
- [CEC.Blazor Server Site](https://cec-blazor-server.azurewebsites.net/) - look at *Modal Weather*.

## The Modal Dialog Classes

There are three classes, one interface and one Enum:

1. `IModal`
2. `BootStrapModal`
3. `ModalOptions`
4. `ModalResult`
5. `ModalResultType`


### IModal

`IModal` defines an interface that all modal dialogs must implementation.

```c#
public interface IModal
{
    ModalOptions Options { get; set; }

    //  Method to display a Modal Dialog
    Task<ModalResult> ShowAsync<TModal>(ModalOptions options) where TModal : IComponent;

    // Method to update the Modal Dialog during display
    void Update(ModalOptions options = null);

    // Method to dismiss - normally called by the dismiss button in the header bar
    void Dismiss();

    // Method to close the dialog - normally called by the child component TModal
    void Close(ModalResult result);
}
```

### ModalResultType

```c#
// Defines the types for exiting the dialog
public enum ModalResultType
{
    NoSet,
    OK,
    Cancel,
    Exit
}
```

### ModalResult

`ModalResult` is passed back to the `Show` caller as the `Task` completion result when the modal closes.

```c#
public class ModalResult
{
    // The closing type
    public BootstrapModalResultType ResultType { get; private set; } = ModalResultType.NoSet;

    // Whatever object you wish to pass back
    public object Data { get; set; } = null;

    // A set of static methods to build a BootstrapModalResult

    public static ModalResult OK() => new ModalResult() {ResultType = ModalResultType.OK };

    public static ModalResult Exit() => new ModalResult() {ResultType = ModalResultType.Exit};

    public static ModalResult Cancel() => new ModalResult() {ResultType = ModalResultType.Cancel };

    public static ModalResult OK(object data) => new ModalResult() { Data = data, ResultType = ModalResultType.OK };

    public static ModalResult Exit(object data) => new ModalResult() { Data = data, ResultType = ModalResultType.Exit };

    public static ModalResult Cancel(object data) => new ModalResult() { Data = data, ResultType = ModalResultType.Cancel };
}
```
### ModalOptions

`ModalOptions` is an options class passed to the Modal Dialog class when opening the Dialog.  The properties are pretty self explanatory.  `Parameters` provides a flexibility way to pass values.
```c#
public class ModalOptions
{
    public string Title { get; set; } = "Modal Dialog";

    public bool ShowCloseButton { get; set; }

    public bool HideHeader { get; set; }

    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

    public bool GetParameter(string key, out object value)
    {
        value = null;
        if (this.Parameters.ContainsKey(key)) value = this.Parameters[key];
        return this.Parameters.ContainsKey(key);
    }

    public object GetParameter(string key)
    {
        if (this.Parameters.ContainsKey(key)) return this.Parameters[key];
        else return null;
    }

    public string GetParameterAsString(string key)
    {
        if (this.Parameters.ContainsKey(key) && this.Parameters[key] is string) return (string)this.Parameters[key];
        else return string.Empty;
    }

    public void SetParameter(string key, object value)
    {
        if (this.Parameters.ContainsKey(key)) this.Parameters[key] = value;
        else this.Parameters.Add(key, value); 
    }
}
```
### BootStrapModal

The Razor Markup for `BootstrapModal` implements Bootstrap markup for a dialog.  No need to worry about toggling the container `display` mode, no content gets rendered when `_ShowDialog` is false.  A cascading value gives child forms access to the instance of ModalDialog. 
```c#
@inherits Component

@namespace CEC.Blazor.Components.UIControls

@if (this._ShowModal)
{
    <CascadingValue Value="this">
        <div class="@this._ContainerCss" data-backdrop="static" tabindex="-1" role="dialog" aria-modal="true" style="display:block;">
            <div class="@this._ModalCss">
                <div class="modal-content">
                    @if (!this.Options.HideHeader)
                    {
                        <div class="@this._ModalHeaderCss">
                            <h5 class="modal-title">@this.Options.Title</h5>
                            @if (this.Options.ShowCloseButton)
                            {
                                <button type="button" class="close" data-dismiss="modal" aria-label="Close" @onclick="this.Dismiss">
                                    <span aria-hidden="true">&times;</span>
                                </button>
                            }
                        </div>
                    }
                    <div class="@this._ModalBodyCss">
                        @this._Content
                    </div>
                </div>
            </div>
        </div>
    </CascadingValue>
}
```

Some key points:
1. The component is initialised when the View is created and added to the RenderTree.  Art this point it empty.
2. There's no need for multiple copies for different forms.  When "hidden" there's no form loaded.  Calling `Show<TForm>`, supplying the component type to display the Form as `TForm`, shows the dialog and initialises an instance of `TForm`.
3. The component hides itself.  Either the child form calls the `BootstrapModal` function `Close` or `BootstrapModal` itself calls  `Dismiss`.  Both actions set the Task to completed, `_ShowModal` to false, clear the content and call `Render`.  With `_ShowModal` false, nothing gets rendered.
3. The component uses a `TaskCompletionSource` object to manage the async behaviour of the component and communicate task status back to the caller.

```c#
    public partial class BootstrapModal : Component, IModal
    {
        /// Modal Options Property
        public ModalOptions Options { get; set; } = new ModalOptions();

        /// Render Fragment for the control content
        private RenderFragment _Content { get; set; }

        /// Property to track the modal state
        private bool _ShowModal { get; set; }

        /// Bootstrap CSS specific properties 

        private string _ContainerCss => $"modal fade show {this.Options.GetParameterAsString("ContainerCSS")}".Trim();

        private string _ModalCss => $"modal-dialog {this.Options.GetParameterAsString("ModalCSS")}".Trim();

        private string _ModalHeaderCss => $"modal-header {this.Options.GetParameterAsString("ModalHeaderCSS")}".Trim();

        private string _ModalBodyCss => $"modal-body {this.Options.GetParameterAsString("ModalBodyCSS")}".Trim();

        /// Independant Task passed to Show callers to track component state
        private TaskCompletionSource<ModalResult> _modalcompletiontask { get; set; } = new TaskCompletionSource<ModalResult>();

        /// Method called to show the component.  Returns a task which is set to complete when Dismiss or Close is called internally
        public Task<ModalResult> ShowAsync<TModal>(ModalOptions options) where TModal : IComponent
        {
            this.Options = options;
            this._modalcompletiontask = new TaskCompletionSource<ModalResult>();
            var i = 0;
            this._Content = new RenderFragment(builder =>
            {
                builder.OpenComponent(i++, typeof(TModal));
                builder.CloseComponent();
            });
            this._ShowModal = true;
            InvokeAsync(Render);
            return _modalcompletiontask.Task;
        }

        /// Method to update the state of the display based on UIOptions
        public void Update(ModalOptions options = null)
        {
            this.Options = options ??= this.Options;
            InvokeAsync(Render);
        }

        /// Method called by the dismiss button to close the dialog
        /// sets the task to complete, show to false and renders the component (which hides it as show is false!)
        public async void Dismiss()
        {
            _ = _modalcompletiontask.TrySetResult(ModalResult.Cancel());
            this._ShowModal = false;
            this._Content = null;
            await InvokeAsync(Render);
        }

        /// Method called by child components through the cascade value of this component
        /// sets the task to complete, show to false and renders the component (which hides it as show is false!)
        public async void Close(ModalResult result)
        {
            _ = _modalcompletiontask.TrySetResult(result);
            this._ShowModal = false;
            this._Content = null;
            await InvokeAsync(Render);
        }
    }
```
## Implementing Bootstrap Modal

### The YesNoModal

The `YesNoModal` is a simple "Are You Sure" modal form.
1. It captures the cascaded parent `IModal` object reference as `Parent`.
2. It calls `Close` which calls `Parent.Close()` to hide the dialog. 
3. It checks for a message parameter in `Parent.Options`.

```html
@inherits Component

@namespace CEC.Blazor.Examples.Components

<div class="container">
    <div class="p-3">
        @((MarkupString)this.Message)
    </div>
    <div class="container text-right p-2">
        <button type="button" class="btn btn-danger" @onclick="(e => this.Close(true))">Exit</button>
        <button type="button" class="btn btn-success" @onclick="(e => this.Close(false))">Cancel</button>
    </div>
</div>
```

```c#
public partial class YesNo : Component
{
    [CascadingParameter]
    public IModal Parent { get; set; }

    [Parameter]
    public string Message { get; set; } = "Are You Sure?";

    protected override Task OnRenderAsync(bool firstRender)
    {
        var message = this.Parent.Options.GetParameterAsString("Message");
        if (!string.IsNullOrEmpty(message)) Message = message;
        return Task.CompletedTask;
    }
    public void Close(bool state)
    {
        if (state) this.Parent.Close(ModalResult.Exit());
        else this.Parent.Close(ModalResult.Cancel());
    }
```

### Form using BootstrapModal

`Index.razor` - in *Components/Views* - shows how to implement the modal dialog.    

> The application is routerless,  controlling "paging" though a `ViewManager`.  *Views* such as `Index` inherit from ViewBase rather than `ComponentBase`,  exposing the `ViewManager` instance as `ViewManager` .  `ViewManager` implements an `IModal` dialog as part of it's base setup.
> 
You open the modal dialog through `Viewmanager.ShowModalAsync<TForm>(modaloptions)`.

I won't show all the `Index` code here, just some relevant snippets.

The following buttons are used to show different modal dialogs.

```html
<button type="button" class="btn btn-info" @onclick="(e => this.CounterDialog())">Counter as a Dialog</button>
<button type="button" class="btn btn-dark" @onclick="(e => this.FetchDataDialog())">Fetch Data as a Dialog</button>
<button type="button" class="btn btn-primary" @onclick="(e => this.AreYouSureAsync())">Are You Sure Dialog</button>
```

The function that opens the FetchData View is shown below.  It's *async* and waits on the `Task` object passed back by the modal dialog to complete.

It:
1. Locks the application so the user can't navigate away it.
2. Builds a `ModalOptions` object.
3. Calls `ShowModalDialogAsync` on the `ViewManager`.  This opens the dialog and renders an instance of `FetchData` as the child content.
4. Waits for the provided task to complete. The Modal Dialog sets the task to complete when it closes.
5. Does nothing on the result.
6. Unlocks the application.

```c#
protected async void FetchDataDialog()
{
    this.ViewManager.LockView();
    var modalOptions = new ModalOptions()
    {
        Title = "Fetch Data in a Dialog",
        HideHeader = false,
        ShowCloseButton = true,
    };
    modalOptions.Parameters.Add("ModalBodyCSS", "p-0");
    modalOptions.Parameters.Add("ModalCSS", "modal-xl");
    var result = await this.ViewManager.ShowModalAsync<FetchData>(modalOptions);
    if (result.ResultType == ModalResultType.Cancel)
    {
        //Do something to stop
    }
    this.ViewManager.UnLockView();
}
```

*Are You Sure* is more of the same, with a slightly different set of `ModalOptions`.

```c#
protected async void AreYouSureAsync()
{
    this.ViewManager.LockView();
    var modalOptions = new ModalOptions()
    {
        Title = "Exit Confirm",
        HideHeader = false,
    };
    modalOptions.Parameters.Add("Message", "Try navigating to another site.");
    var result = await this.ViewManager.ShowModalAsync<YesNo>(modalOptions);
    if (result.ResultType == ModalResultType.Cancel)
    {
        //Do something to stop
    }
    this.ViewManager.UnLockView();
}
```
We use the same `IModal` instance to display different forms.  `ShowAsync` passes the type of form, and `BootstrapModal` does the rest.  The dialog is just a container for whatever form you want displaying.

### Counter.razor

`Counter` demonstrates how to code a form to handle Dialog and View display options.

1. `UITag` is a simple helper component that builds out a HTML `<div>` you can turn on and off.
2. `isModal` controls the display of the exit button by checking `ModalParent`. Not `null` means it's displayed in a modal dialog.

Check the counter in both the modal and full display modes.

```html
@namespace CEC.Blazor.Examples.Components

@inherits ViewBase

<div class="container m-4">
    <h1>Counter</h1>

    <p>Current count: @currentCount</p>

    <button class="btn btn-primary" @onclick="IncrementCount">Click me</button>
</div>

<UITag Tag="div" Show="isModal" Css="container m-1 p-2">
    <UITag Tag="div" Css="container">
        <p>This section only shows when the View is opened in a Modal Dialog</p>
    </UITag>

    <UITag Tag="div" Css="container text-right">
        <button class="btn btn-dark" @onclick="Exit">Exit</button>
    </UITag>
</UITag>
```
```c#
@code {

    [CascadingParameter]
    protected IModal ModalParent { get; set; }

    private int currentCount = 0;

    private bool isModal => this.ModalParent != null;

    private void IncrementCount()
    {
        currentCount++;
    }

    private void Exit()
    {
        if (isModal) this.ModalParent.Close(ModalResult.Exit());
    }
}
```

## Wrap Up

This implementation relies on Bootstrap.  To use another framework, or your own css, create a new modal based `BootstrapModal` that implements IModal and change the markup.

More information on `Component` and `ViewManager` is available [here](https://github.com/ShaunCurtis/CEC.Blazor.Examples/tree/master/Articles)

If your looking for a more complex Modal Dialog with more features, take a look at [Blazored Modal Dialog](https://github.com/Blazored/Modal).
