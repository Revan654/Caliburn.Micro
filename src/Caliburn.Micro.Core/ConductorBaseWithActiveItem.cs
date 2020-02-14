using System.Threading;
using System.Threading.Tasks;

namespace Caliburn.Micro
{
    /// <summary>
    /// A base class for various implementations of <see cref="IConductor"/> that maintain an active item.
    /// </summary>
    /// <typeparam name="T">The type that is being conducted.</typeparam>
    public abstract class ConductorBaseWithActiveItem<T> : ConductorBase<T>, IConductActiveItem where T : class
    {
        /// <summary>
        /// The currently active item.
        /// </summary>
        private T _ActiveItem;

        /// <summary>
        /// The currently active item.
        /// </summary>
        public T ActiveItem
        {
            get => _ActiveItem;
            set => ActivateItemAsync(value, CancellationToken.None);
        }

        /// <summary>
        /// The currently active item.
        /// </summary>
        /// <value></value>
        object IHaveActiveItem.ActiveItem
        {
            get => ActiveItem;
            set => ActiveItem = (T)value;
        }

        /// <summary>
        /// Changes the active item.
        /// </summary>
        /// <param name="Item">The new item to activate.</param>
        /// <param name="Close">Indicates whether or not to close the previous active item.</param>
        /// <param name="Token">The cancellation token to cancel operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected virtual async Task ChangeActiveItemAsync(T Item, bool Close, CancellationToken Token = default)
        {
            await Extensions
                .TryDeactivateAsync(_ActiveItem, Close, Token)
                .ConfigureAwait(false);

            Item = EnsureItem(Item);

            _ActiveItem = Item;
            RaiseChange(nameof(ActiveItem));

            if (IsActive)
                await Extensions
                    .TryActivateAsync(Item, Token)
                    .ConfigureAwait(false);

            OnActivationProcessed(_ActiveItem, true);
        }
    }
}
