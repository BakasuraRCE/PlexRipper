import {
	DownloadTaskDTO,
	FileMergeProgress,
	FolderPathDTO,
	NotificationDTO,
	PlexAccountDTO,
	PlexLibraryDTO,
	PlexServerDTO,
	SettingsModelDTO,
	InspectServerProgress,
	SyncServerProgress,
	LibraryProgress,
	DownloadTaskCreationProgress,
} from '@dto/mainApi';
import IObjectUrl from '@interfaces/IObjectUrl';
import IAlert from '@interfaces/IAlert';

export default interface IStoreState extends SettingsModelDTO {
	accounts: PlexAccountDTO[];
	servers: PlexServerDTO[];
	libraries: PlexLibraryDTO[];
	downloads: DownloadTaskDTO[];
	notifications: NotificationDTO[];
	folderPaths: FolderPathDTO[];
	alerts: IAlert[];
	mediaUrls: IObjectUrl[];
	settings: SettingsModelDTO;
	helpIdDialog: string;
	downloadTaskUpdateList: DownloadTaskDTO[];
	// Progress Service
	libraryProgress: LibraryProgress[];
	fileMergeProgressList: FileMergeProgress[];
	inspectServerProgress: InspectServerProgress[];
	syncServerProgress: SyncServerProgress[];
	downloadTaskCreationProgress: DownloadTaskCreationProgress;
}
