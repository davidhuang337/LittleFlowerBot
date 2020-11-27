﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using isRock.LineBot;
using LittleFlowerBot.Extensions;
using LittleFlowerBot.Models.Caches;
using LittleFlowerBot.Models.Game;
using LittleFlowerBot.Models.Game.BoardGame.ChessGames.ChineseChess;
using LittleFlowerBot.Models.Game.BoardGame.KiGames.Gomoku;
using LittleFlowerBot.Models.Game.BoardGame.KiGames.TicTacToe;
using LittleFlowerBot.Models.Game.GuessNumber;

namespace LittleFlowerBot.Services.EventHandler
{
    public class GameHandler : IEventHandler
    {
        private readonly Dictionary<string, Type> _cmdGameTypeDict = new Dictionary<string, Type>()
        {
            {"玩猜數字", typeof(GuessNumberBoard)},
            {"玩井字遊戲", typeof(TicTacToeBoard)},
            {"玩五子棋", typeof(GomokuBoard)},
            {"玩象棋", typeof(ChineseChessBoard)},
        };
        
        private readonly GameFactory _gameFactory;
        private readonly GameStateCache _gameStateCache;

        public GameHandler(GameFactory gameFactory, GameStateCache gameStateCache)
        {
            _gameFactory = gameFactory;
            _gameStateCache = gameStateCache;
        }

        public async Task Act(Event @event)
        {
            string gameId = @event.SenderId();
            var userId = @event.UserId();
            var text = @event.Text();
            await Act(gameId, userId, text);
        }
        
        public async Task Act(string gameId, string userId, string cmd)
        {
            var gameState = await _gameStateCache.Get(gameId);
            if (gameState != null)
            {
                var game = _gameFactory.CreateGame(gameState.GetType());
                game.GameBoard = gameState;
                game.SenderId = gameId;
                if (game.IsMatch(cmd))
                {
                    game.Act(userId, cmd);
                    if (game.GameBoard.IsGameOver())
                    {
                        await _gameStateCache.Remove(gameId);
                    }
                    else
                    {
                        await _gameStateCache.Set(gameId, game.GameBoard);
                    }
                }
                else if(cmd == "我認輸了" )
                {
                    if (game is GomokuGame gomokuGamegame)
                    {
                        gomokuGamegame.GameOver();
                    } else if (game is TicTacToeGame ticTacToeGame)
                    {
                        ticTacToeGame.GameOver();
                    }

                    await _gameStateCache.Remove(gameId);
                }
            }
            else
            {
                if (IsCreateGameCmd(cmd))
                {
                    var gameType = _cmdGameTypeDict[cmd];
                    var game = _gameFactory.CreateGame(gameType);
                    game.SenderId = gameId;
                    await _gameStateCache.Set(gameId, game.GameBoard);
                    game.StartGame();
                }
            }
        }

        private bool IsCreateGameCmd(string cmd)
        {
            return _cmdGameTypeDict.ContainsKey(cmd);
        }
    }
}