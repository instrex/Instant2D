using Instant2D;
using Instant2D.EC;
using Instant2D.Graphics;
using Instant2D.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Steamworks;
using Steamworks.Data;
using Steamworks.ServerList;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Color = Microsoft.Xna.Framework.Color;

namespace Instant2D.TestGame.Scenes {
    public class SteamNetworkingTest : Scene {
        record TextMenu {
            public string Header;
            public List<TextMenu> Items = new();
            public Action<TextMenu> OnClicked;
            public object Context;

            public TextMenu(string header) {
                Header = header;
            }
        }

        class Player : Component, IUpdate {
            public Friend friend;
            public bool IsLocal;

            SpriteComponent _sprite;
            Image? _image;

            public override void Initialize() {
                Task.Run(async () => {
                    var img = await friend.GetMediumAvatarAsync();

                    if (img != null) {
                        _image = img;
                    }
                });

                _sprite = Entity.AddComponent<SpriteComponent>();
            }

            public void Update(float dt) {
                if (_image is Image avatar) {
                    var texture = new Texture2D(InstantApp.Instance.GraphicsDevice, (int)avatar.Width, (int)avatar.Height);
                    texture.SetData(avatar.Data);

                    _sprite.SetSprite(new Sprite(texture, $"avatar/{friend.Name}"));
                }
            }
        }

        TextMenu _currentMenu;
        float _menuAnim;
        int _selection;

        Lobby _currentLobby;

        public override void Initialize() {
            base.Initialize();

            _currentMenu = Main();
            SteamClient.Init(480);

            SteamMatchmaking.OnLobbyMemberDisconnected += (_, _) => SetMenu(Lobby());
            SteamMatchmaking.OnLobbyMemberJoined += (_, friend) => SetMenu(Lobby());
            SteamMatchmaking.OnLobbyMemberLeave += (_, _) => SetMenu(Lobby());
            SteamMatchmaking.OnLobbyEntered += (_) => SetMenu(Lobby());
            SteamNetworking.OnP2PSessionRequest += steamId => {
                SteamNetworking.AcceptP2PSessionWithUser(steamId);
                Console.WriteLine($"Accepted P2P with {steamId}");
            };
        }

        TextMenu Lobby() => new("Lobby") {
            Items = _currentLobby.Members.Select(f => new TextMenu(f.Name))
                .Append(new TextMenu("Leave") {
                    OnClicked = _ => {
                        _currentLobby.Leave();
                        SetMenu(Main());
                    }
                }).ToList()
        };

        TextMenu Main() => new("Main Menu") {
            Items = {
                new TextMenu("Host...") {
                    OnClicked = async menu => {
                        menu.Header = "Creating lobby...";

                        var lobbyCreateRequest = await SteamMatchmaking.CreateLobbyAsync(4);

                        if (lobbyCreateRequest is not Lobby lobby) {
                            SetMenu(new("Cannot create lobby :(") { Items = { Main() } });
                            return;
                        }

                        lobby.SetData("is_testing_i2d_engine", "1");
                        lobby.SetPublic();
                        lobby.SetJoinable(true);

                        _currentLobby = lobby;
                    }
                },

                new TextMenu("Join...") {
                    OnClicked = async menu => {
                        var lobbies = await SteamMatchmaking.LobbyList
                            .WithMaxResults(10)
                            .WithEqual("is_testing_i2d_engine", 1)
                            .RequestAsync();

                        if (lobbies == null) {
                            SetMenu(new("No lobbies were found :(") { Items = { Main() } });
                            return;
                        }

                        foreach (var lobby in lobbies) {
                            menu.Items.Add(new($"{lobby.Id} {lobby.Owner.Id}") {
                                Context = lobby,
                                OnClicked = async menu => {
                                    var lb = (Lobby)menu.Context;
                                    var result = await lb.Join();

                                    if (result != RoomEnter.Success) {
                                        SetMenu(new("Couldn't join the room :(") { Items = { Main() } });
                                        return;
                                    }

                                    _currentLobby = lb;
                                }
                            });
                        }

                        menu.Items.Add(Main());
                    }
                },

                new TextMenu("Back") {
                    OnClicked = menu => {
                        SteamClient.Shutdown();

                        SceneManager.Switch<Game.MainScene>();
                    }
                }
            }
        };

        void SetMenu(TextMenu menu) {
            _currentMenu = menu;
            _selection = 0;
            _menuAnim = 0;
        }

        public override void Update() {
            base.Update();

            if (_menuAnim <= 1f) {
                _menuAnim += TimeManager.DeltaTime * 6;
            }

            if (_currentMenu != null && _currentMenu.Items.Count > 0) {
                var currentItem = _currentMenu.Items[_selection];
                if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.E) && (currentItem.Items.Count != 0 || currentItem.OnClicked != null)) {
                    _currentMenu = currentItem;
                    currentItem.OnClicked?.Invoke(_currentMenu);
                    _menuAnim = 0;
                }

                if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.W)) {
                    _selection--;
                    if (_selection < 0) {
                        _selection = _currentMenu.Items.Count - 1;
                    }
                }

                if (InputManager.IsKeyPressed(Microsoft.Xna.Framework.Input.Keys.S)) {
                    _selection++;
                    if (_selection >= _currentMenu.Items.Count) {
                        _selection = 0;
                    }
                }
            }
        }

        public override void Render() {
            base.Render();

            if (SteamClient.IsLoggedOn) {
                var drawing = GraphicsManager.Context;

                var anim = MathHelper.Clamp(_menuAnim, 0, 1);

                drawing.Begin(Material.Default, Matrix.Identity);

                drawing.DrawString(_currentMenu.Header, new Vector2(15 - (10 * (1f - anim)), 15), Color.White, new Vector2(3), 0, drawOutline: true);

                if (_currentMenu.Items != null) {
                    for (var i = 0; i < _currentMenu.Items.Count; i++) {
                        drawing.DrawString((_selection == i ? "> " : " ") + _currentMenu.Items[i].Header, new Vector2(15 - (10 * (1f - anim)), 50 + 20 * i), _selection == i ? Color.LightGreen : Color.Gray, new Vector2(2), 0, drawOutline: true);
                    }
                }

                drawing.End();
            }
        }
    }
}
