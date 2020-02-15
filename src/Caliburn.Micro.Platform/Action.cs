using System.Collections.Generic;
using System.Windows;

namespace Caliburn.Micro
{
    /// <summary>
    ///   A host for action related attached properties.
    /// </summary>
    public static class Action 
    {
        private static readonly ILogger Log = LogEntry.Logger(typeof(Action));

        /// <summary>
        /// A property definition representing the target of an <see cref="ActionMessage" /> . The DataContext of the element will be set to this instance.
        /// </summary>
        public static readonly DependencyProperty TargetProperty =
            Extensions.RegisterAttached(
                "Target",
                typeof(object),
                typeof(Action),
                null,
                OnTargetChanged
                );

        /// <summary>
        /// A property definition representing the target of an <see cref="ActionMessage" /> . The DataContext of the element is not set to this instance.
        /// </summary>
        public static readonly DependencyProperty TargetWithoutContextProperty =
            Extensions.RegisterAttached(
                "TargetWithoutContext",
                typeof(object),
                typeof(Action),
                null,
                OnTargetWithoutContextChanged
                );

        /// <summary>
        /// Sets the target of the <see cref="ActionMessage" /> .
        /// </summary>
        /// <param name="d"> The element to attach the target to. </param>
        /// <param name="target"> The target for instances of <see cref="ActionMessage" /> . </param>
        public static void SetTarget(DependencyObject d, object target)
        {
            d.SetValue(TargetProperty, target);
        }

        /// <summary>
        /// Gets the target for instances of <see cref="ActionMessage" /> .
        /// </summary>
        /// <param name="d"> The element to which the target is attached. </param>
        /// <returns> The target for instances of <see cref="ActionMessage" /> </returns>
        public static object GetTarget(DependencyObject d)
        {
            return d.GetValue(TargetProperty);
        }

        /// <summary>
        /// Sets the target of the <see cref="ActionMessage" /> .
        /// </summary>
        /// <param name="d"> The element to attach the target to. </param>
        /// <param name="target"> The target for instances of <see cref="ActionMessage" /> . </param>
        /// <remarks>
        ///   The DataContext will not be set.
        /// </remarks>
        public static void SetTargetWithoutContext(DependencyObject d, object target)
        {
            d.SetValue(TargetWithoutContextProperty, target);
        }

        /// <summary>
        /// Gets the target for instances of <see cref="ActionMessage" /> .
        /// </summary>
        /// <param name="d"> The element to which the target is attached. </param>
        /// <returns> The target for instances of <see cref="ActionMessage" /> </returns>
        public static object GetTargetWithoutContext(DependencyObject d)
        {
            return d.GetValue(TargetWithoutContextProperty);
        }

        ///<summary>
        /// Checks if the <see cref="ActionMessage" /> -Target was set.
        ///</summary>
        ///<param name="element"> DependencyObject to check </param>
        ///<returns> True if Target or TargetWithoutContext was set on <paramref name="element" /> </returns>
        public static bool HasTargetSet(DependencyObject element)
        {
            return !(GetTarget(element) is null)
                || !(GetTargetWithoutContext(element) is null)
                ? true
                : !(element is FrameworkElement frameworkElement)
                ? false
                : ConventionManager.HasBinding(frameworkElement, TargetProperty) 
                || ConventionManager.HasBinding(frameworkElement, TargetWithoutContextProperty);
        }

        ///<summary>
        /// Uses the action pipeline to invoke the method.
        ///</summary>
        ///<param name="target"> The object instance to invoke the method on. </param>
        ///<param name="methodName"> The name of the method to invoke. </param>
        ///<param name="view"> The view. </param>
        ///<param name="source"> The source of the invocation. </param>
        ///<param name="eventArgs"> The event args. </param>
        ///<param name="parameters"> The method parameters. </param>
        public static void Invoke(object target, string methodName, DependencyObject view = null, FrameworkElement source = null, object eventArgs = null, IEnumerable<object> parameters = null)
        {
            var message = new ActionMessage { MethodName = methodName };

            using var context = new ActionExecutionContext
            {
                Target = target,

                Method = target?.GetType().GetMethod(methodName),

                Message = message,
                View = view,
                Source = source,
                EventArgs = eventArgs
            };

            foreach (var item in parameters)
            {
                context.Message.Parameters.Add(item as Parameter ?? new Parameter { Value = item });
            }

            ActionMessage.InvokeAction(context);

            Log.Info("Invoking action {0} on {1}.", message.MethodName, target);
        }

        private static void OnTargetWithoutContextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetTargetCore(e, d, false);
        }

        private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetTargetCore(e, d, true);
        }

        private static void SetTargetCore(DependencyPropertyChangedEventArgs e, DependencyObject d, bool setContext)
        {
            if (e.NewValue == e.OldValue || Execute.InDesignMode && e.NewValue is string)
            {
                return;
            }

            var target = e.NewValue;

            if (setContext && d is FrameworkElement element)
            {
                Log.Info("Setting DC of {0} to {1}.", d, target);
                element.DataContext = target;
            }

            Log.Info("Attaching message handler {0} to {1}.", target, d);
            Message.SetHandler(d, target);
        }
    }
}
