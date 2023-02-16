using System;
using System.Collections.ObjectModel;
using System.Reflection;

namespace SymlinkWatcher;

public class FileSystemWatcher : IDisposable
{
	private static readonly Type MainType; // FileSystemWatcher
	private object _instance;

	static FileSystemWatcher()
	{
		try
		{
			var assembly = Assembly.Load( "System.IO.FileSystem.Watcher" );
			if ( assembly == null )
				throw new NullReferenceException( "Assembly System.IO.FileSystem.Watcher == null" );
			MainType = assembly.GetType( "System.IO.FileSystemWatcher" );
		}
		catch ( Exception e )
		{
			// without these static try blocks s&box seems to not load the tool??
		}
	}

	public FileSystemWatcher( string path )
	{
		_instance = Activator.CreateInstance( MainType, path );
		HookEvents();
	}

	public FileSystemWatcher( string path, string filter )
	{
		_instance = Activator.CreateInstance( MainType, path, filter );
		HookEvents();
	}

	#region Event Handling

	private void HookEvents()
	{
		void Hook( string name )
		{
			var eventInfo = MainType.GetEvent( name );
			if ( eventInfo == null )
				throw new NullReferenceException( $"Event {name} == null" );
			var eventHandlerType = eventInfo.EventHandlerType;
			if ( eventHandlerType == null )
				throw new NullReferenceException( $"Event {name}.EventHandlerType == null" );
			var selfEventHandler =
				GetType().GetMethod( $"Handle{name}Event", BindingFlags.Instance | BindingFlags.NonPublic );
			if ( selfEventHandler == null )
				throw new NullReferenceException( $"Method Handle{name}Event == null" );
			var methodDelegate = Delegate.CreateDelegate( eventHandlerType, this, selfEventHandler );
			eventInfo.AddEventHandler( _instance, methodDelegate );
		}

		Hook( "Changed" );
		Hook( "Created" );
		Hook( "Deleted" );
		Hook( "Error" );
		Hook( "Renamed" );
	}

	public EventHandler Changed;
	public EventHandler Created;
	public EventHandler Deleted;
	public EventHandler Error;
	public EventHandler Renamed;

	private void HandleChangedEvent( object sender, EventArgs args ) => Changed?.Invoke( this, args );
	private void HandleCreatedEvent( object sender, EventArgs args ) => Created?.Invoke( this, args );
	private void HandleDeletedEvent( object sender, EventArgs args ) => Deleted?.Invoke( this, args );
	private void HandleErrorEvent( object sender, EventArgs args ) => Error?.Invoke( this, args );
	private void HandleRenamedEvent( object sender, EventArgs args ) => Renamed?.Invoke( this, args );

	#endregion

	#region Properties

	public string Filter
	{
		get => (string)MainType.GetProperty( "Filter" )?.GetValue( _instance );
		set => MainType.GetProperty( "Filter" )?.SetValue( _instance, value );
	}

	public Collection<string> Filters
	{
		get => (Collection<string>)MainType.GetProperty( "Filters" )?.GetValue( _instance );
		set => MainType.GetProperty( "Filters" )?.SetValue( _instance, value );
	}

	public bool IncludeSubdirectories
	{
		get
		{
			var property = MainType.GetProperty( "IncludeSubdirectories" );
			if ( property == null )
				throw new NullReferenceException( "Property IncludeSubdirectories == null" );
			return (bool)property.GetValue( _instance )!;
		}
		set
		{
			var property = MainType.GetProperty( "IncludeSubdirectories" );
			if ( property == null )
				throw new NullReferenceException( "Property IncludeSubdirectories == null" );
			property.SetValue( _instance, value );
		}
	}

	public bool EnableRaisingEvents
	{
		get => (bool)MainType.GetProperty( "EnableRaisingEvents" )?.GetValue( _instance )!;
		set => MainType.GetProperty( "EnableRaisingEvents" )?.SetValue( _instance, value );
	}

	public Internal.NotifyFilters NotifyFilter
	{
		get => (Internal.NotifyFilters)MainType.GetProperty( "NotifyFilter" )?.GetValue( _instance )!;
		set => MainType.GetProperty( "NotifyFilter" )?.SetValue( _instance, value );
	}

	#endregion

	public void Dispose()
	{
		MainType.GetMethod( "Dispose" )?.Invoke( _instance, null );
		_instance = null;
	}
}
