using CEC.Blazor.Examples.Client.Components;
using Microsoft.AspNetCore.Components;

namespace CEC.Blazor.Examples.Components
{
    public partial class Index : ViewBase
    {
        private bool isLocked = false;

        private string buttonLabel => isLocked ? "Unlock" : "Lock";

        private string buttonStatus => isLocked ? "Locked" : "Unlocked";

        private string buttoncss => isLocked ? "btn-danger" : "btn-success";

        private string alertcss => isLocked ? "alert-danger" : "alert-success";


        private void SwitchLock()
        {
            isLocked = !isLocked;
            if (isLocked) this.ViewManager.LockView();
            else this.ViewManager.UnLockView();
        }

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

        protected async void CounterDialog()
        {
            this.ViewManager.LockView();
            var modalOptions = new ModalOptions()
            {
                Title = "Counter in a Dialog",
                HideHeader = false,
                ShowCloseButton = true,
            };
            modalOptions.Parameters.Add("ModalBodyCSS", "p-0");
            modalOptions.Parameters.Add("ModalCSS", "modal-xl");
            var result = await this.ViewManager.ShowModalAsync<Counter>(modalOptions);
            if (result.ResultType == ModalResultType.Cancel)
            {
                //Do something to stop
            }
            this.ViewManager.UnLockView();
        }

    }
}
