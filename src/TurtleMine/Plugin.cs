using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Interop.BugTraqProvider;
using TurtleMine.Resources;
using TurtleMineShared.Utils;

namespace TurtleMine
{
    /// <summary>
    /// Redmine Plugin
    /// </summary>
    [ComVisible(true)]
#if WIN64
    [Guid("55B7DC40-2D4A-46AB-8884-329A02D26EDF")]
#else
    [Guid("55B7DC40-2D4A-46AB-8884-329A02D26EDE")]
#endif
    [ClassInterface(ClassInterfaceType.None), ProgId("TurtleMine")]
    public sealed class Plugin : IBugTraqProvider2
    {
        #region Members

        private FeedParser _parser;

        #endregion

        #region Constructor

        ///// <summary>
        ///// Initializes a new instance of the <see cref="PluginRedMine"/> class.
        ///// </summary>
        //public PluginRedMine()
        //{
        //    noItemPerPage = 100; // 25,50,100
        //    keyItemPerPage = "&per_page=";
        //    //keyNumPage = "&page=";
        //}
        #endregion

        #region Properties

        private string feedsUrl { get; set; }

        /// <summary>
        /// Gets the feedItems.
        /// </summary>
        /// <value>The feedItems.</value>
        internal List<FeedItem> FeedItems
        {
            get { return _parser != null ? _parser.FeedItems : new List<FeedItem>(); }
        }

        /// <summary>
        /// Gets the feed title.
        /// </summary>
        /// <value>The feed title.</value>
        internal string FeedTitle
        {
            get
            {
                return _parser != null ? _parser.FeedTitle : String.Empty;
                
            }
        }

        /// <summary>
        /// Gets a value indicating whether the [feed failed to load].
        /// </summary>
        /// <value><c>true</c> if [feed failed to load]; otherwise, <c>false</c>.</value>
        internal bool FeedFailedToLoad { get; private set; }
        #endregion

        #region Methods for TSVN
        
        /// <summary>
        /// Validates the parameters.
        /// </summary>
        /// <param name="hParentWnd">Parent window for any UI that needs to be displayed during validation.</param>
        /// <param name="parameters">The parameter string that needs to be validated.</param>
        /// <returns>Is the string valid?</returns>
        public bool ValidateParameters(IntPtr hParentWnd, string parameters)
        {
            return true;
        }

        /// <summary>
        /// Gets the link text.
        /// </summary>
        /// <param name="hParentWnd">Parent window for any (error) UI that needs to be displayed.</param>
        /// <param name="parameters">The parameter string, just in case you need to talk to your web service (e.g.) to find out what the correct text is.</param>
        /// <returns>What text do you want to display? Use the current thread locale.</returns>
        public string GetLinkText(IntPtr hParentWnd, string parameters)
        {
            return Strings.ButtonText;
        }

        /// <summary>
        /// Gets the commit message.
        /// </summary>
        /// <param name="hParentWnd">Parent window for your provider's UI.</param>
        /// <param name="parameters">Parameters for your provider.</param>
        /// <param name="commonRoot">The common root.</param>
        /// <param name="pathList">The path list.</param>
        /// <param name="originalMessage">The text already present in the commit message. Your provider should include this text in the new message, where appropriate.</param>
        /// <returns>The new text for the commit message. This replaces the original message.</returns>
        /// <exception cref="Exception"><c>Exception</c>.</exception>
        public string GetCommitMessage(IntPtr hParentWnd, string parameters, string commonRoot, string[] pathList, string originalMessage)
        {
            try
            {
                feedsUrl = parameters;

                //Show form
                var form = new IssuesForm(this, originalMessage);

                //If don't click ok then return the original message.
                if (form.ShowDialog() != DialogResult.OK)
                {
                    return originalMessage;
                }

                //If we click ok but no items are selected then return the original message.
                if (form.ItemsFixed.Count <= 0)
                {
                    return originalMessage;
                }

                //Add issues to comment window
                var result = new StringBuilder();
                var staffName = ConfigHelper.GetStaffName();

                // 添加原始消息
                if (originalMessage.Length > 0)
                {
                    result.AppendLine(originalMessage);
                }

                // 为每个选中的问题添加新格式的信息
                foreach (var item in form.ItemsFixed)
                {
                    result.AppendLine($"@redmine单号:#{item.Number}");
                    result.AppendLine($"@提交者:{staffName}");
                    result.AppendLine($"@提交说明: {item.Description}");
                    // result.AppendLine(); // 添加空行分隔
                }

                return result.ToString();
            }
            catch (Exception ex)
            {
                var dialog = new ErrorDialog(ex, ErrorDialog.ButtonState.CloseContinue);
                dialog.ShowDialog(AppWindow);
                return originalMessage;
            }
        }

        /// <summary>
        /// Gets the commit message2.
        /// </summary>
        /// <param name="hParentWnd">Parent window for your provider's UI.</param>
        /// <param name="parameters">Parameters for your provider.</param>
        /// <param name="commonUrl">The common URL of the commit.</param>
        /// <param name="commonRoot">The common root.</param>
        /// <param name="pathList">The path list.</param>
        /// <param name="originalMessage">The text already present in the commit message. Your provider should include this text in the new message, where appropriate.</param>
        /// <param name="bugID">The content of the <paramref name="bugID"/> field (if shown)</param>
        /// <param name="bugIDOut">Modified content of the <paramref name="bugID"/> field.</param>
        //You can assign custom revision properties to a commit by setting the next two params.  note: Both safearrays must be of the same length.   For every property name there must be a property value!
        /// <param name="revPropNames">The list of revision property names.</param>
        /// <param name="revPropValues">The list of revision property values.</param>
        /// <returns>The new text for the commit message. This replaces the original message.</returns>
        public string GetCommitMessage2(IntPtr hParentWnd, string parameters, string commonUrl, string commonRoot, string[] pathList, string originalMessage, string bugID, out string bugIDOut, out string[] revPropNames, out string[] revPropValues)
        {
            bugIDOut = bugID;

            // If no revision properties are to be set, 
            // the plug-in MUST return empty arrays. 
            revPropNames = new string[0];
            revPropValues = new string[0];

            return GetCommitMessage(hParentWnd, parameters, commonRoot, pathList, originalMessage);
        }

        /// <summary>
        /// Checks the commit.
        /// </summary>
        /// <param name="hParentWnd">The h parent WND.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="commonUrl">The common URL.</param>
        /// <param name="commonRoot">The common root.</param>
        /// <param name="pathList">The path list.</param>
        /// <param name="commitMessage">The commit message.</param>
        /// <returns></returns>
        public string CheckCommit(IntPtr hParentWnd, string parameters, string commonUrl, string commonRoot, string[] pathList, string commitMessage)
        {
            return null;
        }

        /// <summary>
        /// Called when [commit finished].
        /// </summary>
        /// <param name="hParentWnd">Parent window for any (error) UI that needs to be displayed.</param>
        /// <param name="commonRoot">The common root of all paths that got committed.</param>
        /// <param name="pathList">All the paths that got committed.</param>
        /// <param name="logMessage">The text already present in the commit message.</param>
        /// <param name="revision">The revision of the commit.</param>
        /// <returns>An error to show to the user if this function returns something else than S_OK</returns>
        public string OnCommitFinished(IntPtr hParentWnd, string commonRoot, string[] pathList, string logMessage, int revision)
        {
            return null;
        }

        /// <summary>
        /// Whether the provider provides options
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance has options; otherwise, <c>false</c>.
        /// </returns>
        public bool HasOptions()
        {
            return true;
        }

        /// <summary>
        /// Shows the options dialog.
        /// </summary>
        /// <param name="hParentWnd">Parent window for the options dialog.</param>
        /// <param name="parameters">Parameters for your provider.</param>
        /// <returns>The parameters string</returns>
        public string ShowOptionsDialog(IntPtr hParentWnd, string parameters)
        {
            var dialog = new OptionsDialog {Parameter = parameters};

            return dialog.ShowDialog(WindowWrapper.TryCreate(hParentWnd)) == DialogResult.OK ? dialog.Parameter : parameters;
        }

        #endregion

        /// <summary>
        /// Gets the ATOM Feed.
        /// </summary>
        internal void GetFeed()
        {
            try
            {
                _parser = new FeedParser(feedsUrl);
            }
            catch (Exception ex)
            {
                var dialog = new ErrorDialog(Strings.ErrorReadingIssuesListTitle, String.Format(Strings.ErrorReadingIssuesListMessage, Environment.NewLine + ex.Message), ex, ErrorDialog.ButtonState.CloseContinue);
                dialog.ShowDialog(AppWindow);

                FeedFailedToLoad = true;
            }
        }

        private static IWin32Window _appWindow;
        /// <summary>
        /// Gets the app window for the Issues form.
        /// </summary>
        /// <value>The app window.</value>
        public static IWin32Window AppWindow
        {
            get { return _appWindow ?? (_appWindow = WindowWrapper.TryCreate(Application.OpenForms[0].Handle)); }
        }   
    }

    /// <summary>
    /// Convert an <c>IntPtr</c> to an Windows handle
    /// </summary>
    internal sealed class WindowWrapper : IWin32Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowWrapper"/> class.
        /// </summary>
        /// <param name="handle">The handle.</param>
        private WindowWrapper(IntPtr handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// Gets the handle to the window represented by the implementer.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// A handle to the window represented by the implementer.
        /// </returns>
        public IntPtr Handle { get; private set; }

        /// <summary>
        /// Tries to create a windows wrapper from the hwnd.
        /// </summary>
        /// <param name="handle">The handle.</param>
        /// <returns>A handle to the window represented by the implementer.</returns>
        public static WindowWrapper TryCreate(IntPtr handle)
        {
            return handle != IntPtr.Zero ? new WindowWrapper(handle) : null;
        }
    }
}