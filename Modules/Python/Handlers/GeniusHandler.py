from main import FinBot
import lyricsgenius as genius
from Data import config

# TODO:
"""
Neaten up get_track_by_lyrics error handling
"""


class GeniusSearcher:
    def __init__(self, bot: FinBot):
        self.bot: FinBot = bot
        self.Genius = None
        self.ready = False
        bot.loop.run_in_executor(None, self.authenticate)

    def authenticate(self):
        genius_config = genius.Genius(config.genius_id)
        genius_config.verbose = False  # Turn off status messages
        genius_config.remove_section_headers = True  # Remove section headers (e.g. [Chorus]) from lyrics when searching
        genius_config.excluded_terms = ["(Remix)", "(Live)", "(cover)"]  # Exclude songs with these words in their title
        self.Genius = genius_config
        self.ready = True

    def get_track(self, song):
        song_lyrics = self.Genius.search_song(song)

        if song_lyrics is not None:
            return song_lyrics
        else:
            return None

    def get_track_by_lyrics(self, lyrics):
        """
        This really isn't pretty at the moment, but it stops errors from being thrown.
        """
        try:
            request = self.Genius.search_all(lyrics)
            first_run = True
            for hit in request['sections'][2]['hits']:
                if first_run:
                    first_hit = hit["result"]
                    first_run = False

            if first_hit is not None:
                return first_hit
            else:
                return None
        except:
            return None