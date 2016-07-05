// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// An abstraction for holding and aggregating exceptions.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

// Disable the "reference to volatile field not treated as volatile" error.

using System.Collections.Generic;

#pragma warning disable 0420

namespace System.Threading.Tasks {
  using System;
  using System.Collections.ObjectModel;
  using System.Diagnostics.Contracts;
  using System.Security;

  /// <summary>
  /// An exception holder manages a list of exceptions for one particular task.
  /// It offers the ability to aggregate, but more importantly, also offers intrinsic
  /// support for propagating unhandled exceptions that are never observed. It does
  /// this by aggregating and throwing if the holder is ever GC'd without the holder's
  /// contents ever having been requested (e.g. by a Task.Wait, Task.get_Exception, etc).
  /// This behavior is prominent in .NET 4 but is suppressed by default beyond that release.
  /// </summary>
  internal class TaskExceptionHolder {
    /// <summary>Whether we should propagate exceptions on the finalizer.</summary>
    private readonly static bool s_failFastOnUnobservedException = ShouldFailFastOnUnobservedException();
    /// <summary>Whether the AppDomain has started to unload.</summary>
    private static volatile bool s_domainUnloadStarted;
    /// <summary>An event handler used to notify of domain unload.</summary>
    private static volatile EventHandler s_adUnloadEventHandler;

    /// <summary>The task with which this holder is associated.</summary>
    private readonly Task m_task;

    /// <summary>Whether the holder was "observed" and thus doesn't cause finalization behavior.</summary>
    private volatile bool m_isHandled;

    /// <summary>
    /// Creates a new holder; it will be registered for finalization.
    /// </summary>
    /// <param name="task">The task this holder belongs to.</param>
    internal TaskExceptionHolder( Task task ) {
      Contract.Requires( task != null, "Expected a non-null task." );
      m_task = task;
      EnsureADUnloadCallbackRegistered();
    }

    [SecuritySafeCritical]
    private static bool ShouldFailFastOnUnobservedException() {
      bool shouldFailFast = false;
#if !FEATURE_CORECLR
      shouldFailFast = System.CLRConfig.CheckThrowUnobservedTaskExceptions();
#endif
      return shouldFailFast;
    }

    private static void EnsureADUnloadCallbackRegistered() {
      if ( s_adUnloadEventHandler == null &&
          Interlocked.CompareExchange( ref s_adUnloadEventHandler,
                                       AppDomainUnloadCallback,
                                       null ) == null ) {
        AppDomain.CurrentDomain.DomainUnload += s_adUnloadEventHandler;
      }
    }

    private static void AppDomainUnloadCallback( object sender, EventArgs e ) {
      s_domainUnloadStarted = true;
    }


    /// <summary>Gets whether the exception holder is currently storing any exceptions for faults.</summary>
    internal bool ContainsFaultList { get { return false; } }

    /// <summary>
    /// Add an exception to the holder.  This will ensure the holder is
    /// in the proper state (handled/unhandled) depending on the list's contents.
    /// </summary>
    /// <param name="exceptionObject">
    /// An exception object (either an Exception, an ExceptionDispatchInfo,
    /// an IEnumerable{Exception}, or an IEnumerable{ExceptionDispatchInfo}) 
    /// to add to the list.
    /// </param>
    /// <remarks>
    /// Must be called under lock.
    /// </remarks>
    internal void Add( object exceptionObject ) {
      Add( exceptionObject, representsCancellation: false );
    }

    /// <summary>
    /// Add an exception to the holder.  This will ensure the holder is
    /// in the proper state (handled/unhandled) depending on the list's contents.
    /// </summary>
    /// <param name="representsCancellation">
    /// Whether the exception represents a cancellation request (true) or a fault (false).
    /// </param>
    /// <param name="exceptionObject">
    /// An exception object (either an Exception, an ExceptionDispatchInfo,
    /// an IEnumerable{Exception}, or an IEnumerable{ExceptionDispatchInfo}) 
    /// to add to the list.
    /// </param>
    /// <remarks>
    /// Must be called under lock.
    /// </remarks>
    internal void Add( object exceptionObject, bool representsCancellation ) {
    }

    /// <summary>
    /// A private helper method that ensures the holder is considered
    /// handled, i.e. it is not registered for finalization.
    /// </summary>
    /// <param name="calledFromFinalizer">Whether this is called from the finalizer thread.</param> 
    internal void MarkAsHandled( bool calledFromFinalizer ) {
      if ( !m_isHandled ) {
        if ( !calledFromFinalizer ) {
          GC.SuppressFinalize( this );
        }

        m_isHandled = true;
      }
    }

    /// <summary>
    /// Allocates a new aggregate exception and adds the contents of the list to
    /// it. By calling this method, the holder assumes exceptions to have been
    /// "observed", such that the finalization check will be subsequently skipped.
    /// </summary>
    /// <param name="calledFromFinalizer">Whether this is being called from a finalizer.</param>
    /// <param name="includeThisException">An extra exception to be included (optionally).</param>
    /// <returns>The aggregate exception to throw.</returns>
    internal AggregateException CreateExceptionObject( bool calledFromFinalizer, Exception includeThisException ) {

      return new AggregateException( new List<Exception>() );
    }
  }
}
