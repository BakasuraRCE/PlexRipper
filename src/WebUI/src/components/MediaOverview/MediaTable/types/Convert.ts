import ITreeViewItem from '@mediaOverview/MediaTable/types/iTreeViewItem';
import { PlexMediaType, PlexMovieDTO, PlexTvShowDTO } from '@dto/mainApi';

export default abstract class Convert {
	public static tvShowsToTreeViewItems(tvShows: PlexTvShowDTO[]): ITreeViewItem[] {
		const items: ITreeViewItem[] = [];

		tvShows.forEach((tvShow: PlexTvShowDTO) => {
			const seasons: ITreeViewItem[] = [];
			if (tvShow.seasons) {
				tvShow.seasons.forEach((season) => {
					const episodes: ITreeViewItem[] = [];
					if (season.episodes) {
						season.episodes.forEach((episode) => {
							// Add Episode
							episodes.push({
								id: episode.id,
								key: `${tvShow.id}-${season.id}-${episode.id}`,
								title: episode.title ?? '',
								type: PlexMediaType.Episode,
								children: [],
								item: episode,
								addedAt: episode.addedAt ?? '',
								updatedAt: episode.updatedAt ?? '',
							});
						});
						// Add seasons
						seasons.push({
							id: season.id,
							key: `${tvShow.id}-${season.id}`,
							title: season.title ?? '',
							type: PlexMediaType.Season,
							children: episodes,
							item: season,
							addedAt: season.addedAt ?? '',
							updatedAt: season.updatedAt ?? '',
						});
					}
				});
				// Add tvShow
				items.push({
					id: tvShow.id,
					key: `${tvShow.id}`,
					title: tvShow.title ?? '',
					year: tvShow.year,
					type: PlexMediaType.TvShow,
					item: tvShow,
					children: seasons,
					addedAt: tvShow.addedAt ?? '',
					updatedAt: tvShow.updatedAt ?? '',
				});
			}
		});

		return items;
	}

	public static moviesToTreeViewItems(movies: PlexMovieDTO[]): ITreeViewItem[] {
		const items: ITreeViewItem[] = [];

		movies.forEach((movie: PlexMovieDTO) => {
			if (movie) {
				// Add tvShow
				items.push({
					id: movie.id,
					key: `${movie.id}`,
					title: movie.title ?? '',
					year: movie.year,
					type: PlexMediaType.Movie,
					item: movie,
					children: [],
					addedAt: movie.addedAt ?? '',
					updatedAt: movie.updatedAt ?? '',
				});
			}
		});
		return items;
	}
}
