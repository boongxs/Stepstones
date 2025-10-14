using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using static stepstones.Resources.AppConstants;

namespace stepstones.ViewModels
{
    public partial class Paginator : ObservableObject
    {
        private readonly Func<int, Task> _loadPageAction;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PageInfo))]
        [NotifyCanExecuteChangedFor(nameof(GoToPreviousPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(GoToFirstPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(GoToNextPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(GoToLastPageCommand))]
        private int _currentPage = 1;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PageInfo))]
        [NotifyCanExecuteChangedFor(nameof(GoToNextPageCommand))]
        [NotifyCanExecuteChangedFor(nameof(GoToLastPageCommand))]
        private int _totalPages = 1;

        [ObservableProperty]
        private int _pageSize = DefaultPageSize;

        public string PageInfo => string.Format(PageInfoFormat, CurrentPage, TotalPages);

        public Paginator(Func<int, Task> loadPageAction)
        {
            _loadPageAction = loadPageAction ?? throw new ArgumentNullException(nameof(loadPageAction));
        }

        public void UpdateTotalPages(int totalItems)
        {
            if (totalItems == 0)
            {
                TotalPages = 1;
                CurrentPage = 1;
                return;
            }

            TotalPages = (int)Math.Ceiling((double)totalItems / PageSize);
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
            }
        }

        [RelayCommand(CanExecute = nameof(CanGoToPreviousPage))]
        private async Task GoToPreviousPage()
        {
            CurrentPage--;
            await _loadPageAction(CurrentPage);
        }

        private bool CanGoToPreviousPage() => CurrentPage > 1;

        [RelayCommand(CanExecute = nameof(CanGoToNextPage))]
        private async Task GoToNextPage()
        {
            CurrentPage++;
            await _loadPageAction(CurrentPage);
        }

        private bool CanGoToNextPage() => CurrentPage < TotalPages;

        [RelayCommand(CanExecute = nameof(CanGoToFirstPage))]
        private async Task GoToFirstPage()
        {
            CurrentPage = 1;
            await _loadPageAction(CurrentPage);
        }

        private bool CanGoToFirstPage() => CurrentPage > 1;

        [RelayCommand(CanExecute = nameof(CanGoToLastPage))]
        private async Task GoToLastPage()
        {
            CurrentPage = TotalPages;
            await _loadPageAction(CurrentPage);
        }

        private bool CanGoToLastPage() => CurrentPage < TotalPages;
    }
}
