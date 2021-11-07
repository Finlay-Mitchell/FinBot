using Discord.Commands;
using System.Threading.Tasks;

namespace FinBot.Modules
{
    public class GameCommands : ModuleBase<ShardedCommandContext>
    {
        [Command("Chess_status"), Summary("Gets the current status of a chess current game."), Remarks("(PREFIX)chess_status <game id>"), Alias("chessstatus", "chess_stats", "chessstats")]
        public Task ChessStatus(params string[] args)
        {
            return Task.CompletedTask;
        }
        
        [Command("chess_accept"), Summary("Accepts a chess offering"), Remarks("(PREFIX)chess_accept <game id>"), Alias("chessaccept", "acceptchess", "accept_chess")]
        public Task ChessAccept(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("chess_move"), Summary("Moves a piece on your chess board"), Remarks("(PREFIX)chess_move <SAN notation> <game id>"), Alias("chessmove", "move_chess", "movechess")]
        public Task ChessMove(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("chess_offer"), Summary("Makes an offer for the chess game"), Remarks("(PREFIX)chess_offer <action(DRAW, UNDO)> <game id>"), Alias("chessoffer", "offer_chess", "offerchess")]
        public Task ChessOffer(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("chess_play"), Summary("Initiates a new game of chess with a player."), Remarks("(PREFIX)chess_play <user>"), Alias("chessplay", "playchess", "play_chess")]
        public Task ChessPlay(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("chess_concede"), Summary("Concedes the current game of chess."), Remarks("(PREFIX)chess_concede <game id>"), Alias("chessconcede", "concede_chess", "concedechess")]
        public Task ChessConcede(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("chess_games"), Summary("Gets a list of your chess games."), Remarks("(PREFIX)chess_games (optional)<all>"), Alias("chessgames", "chessgame", "chess_game")]
        public Task ChessGames(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("chess_elo"), Summary("Gets your chess elo rating."), Remarks("(PREFIX)chess_elo"), Alias("chesselo", "elochess", "elo_chess")]
        public Task ChessElo(params string[] args)
        {
            return Task.CompletedTask;
        }

        [Command("chess_leaderboard"), Summary("Gets the global chess leaderboard."), Remarks("(PREFIX)chess_leaderboard (optional)<# of results>"), Alias("chessleaderboard")]
        public Task ChessLeaderboard(params string[] args)
        {
            return Task.CompletedTask;
        }
    }
}
