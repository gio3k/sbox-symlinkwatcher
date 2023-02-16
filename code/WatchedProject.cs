using System;
using System.Collections.Generic;
using System.IO;
using Sandbox;

namespace SymlinkWatcher;

public class WatchedProject : IDisposable
{
	public LocalProject LocalProject { get; }
	private object ServerContent { get; }
	private object ToolAddon { get; }
	public List<FileSystemWatcher> WatchedFolders { get; } = new();

	public WatchedProject( LocalProject project )
	{
		LocalProject = project;

		var path = project.GetCodePath();
		if ( string.IsNullOrEmpty( path ) )
			return;

		var fullIdent = project.Config.FullIdent;

		ServerContent = Internal.GetServerContentByIdent( fullIdent );
		if ( ServerContent == null )
			ToolAddon = Internal.GetToolAddonByIdent( fullIdent );

		foreach ( var directoryPath in Directory.GetDirectories( path, "*", SearchOption.TopDirectoryOnly ) )
		{
			var directory = new DirectoryInfo( directoryPath );
			if ( directory.LinkTarget == null )
				continue; // not a symlink - skip!
			var resolved = directory.ResolveLinkTarget( true );
			if ( resolved == null )
				throw new NullReferenceException( $"Resolved path for symlink {directoryPath} == null" );
			if ( !resolved.Exists )
			{
				Log.Warning( $"Broken symlink {directoryPath} -/> {resolved.FullName}" );
				continue;
			}

			var watcher = new FileSystemWatcher( resolved.FullName );
			watcher.Filters.Add( "*.cs" );
			watcher.Filters.Add( "*.razor" );
			watcher.Filters.Add( "*.scss" );
			watcher.NotifyFilter = Internal.NotifyFilters.Attributes | Internal.NotifyFilters.DirectoryName |
			                       Internal.NotifyFilters.FileName |
			                       Internal.NotifyFilters.LastAccess | Internal.NotifyFilters.LastWrite |
			                       Internal.NotifyFilters.Size;
			Log.Info( $"{LocalProject.Config.Ident}: Watching symlink {directoryPath} -> {resolved.FullName}" );
			watcher.Changed += Recompile;
			watcher.Created += Recompile;
			watcher.Deleted += Recompile;
			watcher.Renamed += Recompile;
			watcher.IncludeSubdirectories = true;
			watcher.EnableRaisingEvents = true;
			WatchedFolders.Add( watcher );
		}
	}

	private void Recompile( object sender, EventArgs args ) => Recompile();

	private void Recompile()
	{
		var compilers = new List<object>();
		if ( ServerContent != null )
		{
			if ( ServerContent.GetType().Name != "RuntimeContent" )
				return;
			compilers.Add( Internal.GetRuntimeContentCompiler( ServerContent ) );
		}

		if ( ToolAddon != null )
		{
			compilers.Add( Internal.GetToolAddonCompiler( ToolAddon ) );
		}

		foreach ( var compiler in compilers )
		{
			Internal.MarkForRecompile( compiler );
		}
	}

	public void Dispose()
	{
		foreach ( var fileSystemWatcher in WatchedFolders ) fileSystemWatcher.Dispose();
	}
}
