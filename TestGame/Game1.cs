using Instant2D;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TestGame
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();

            manBackpackTrans.Parent = manTrans;
            manHatTrans.Parent = manTrans;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            texture = Content.Load<Texture2D>("pixel");
            manpng = Content.Load<Texture2D>("man");
            // TODO: use this.Content to load your game content here
        }

        Transform trans = new() { Position = new Vector2(50), Scale = new Vector2(2, 1) };
        Transform trans2 = new() { Position = new Vector2(50), Scale = new Vector2(21, 14) };
        Texture2D texture, manpng;

        Transform manTrans = new() { Position = new Vector2(50) };
        Transform manBackpackTrans = new() { Position = new Vector2(50) };
        Transform manHatTrans = new() { Position = new Vector2(50) };

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            trans.Position += new Vector2(2, 0);
            trans.Rotation += 0.07f;
            trans2.Parent = trans;
            trans2.LocalPosition = new Vector2(25, 0);
            trans2.Rotation = 0f;

            var state = Keyboard.GetState();

            if (state.IsKeyDown(Keys.D))
                manTrans.Position += new Vector2(2, 0);

            if (state.IsKeyDown(Keys.A))
                manTrans.Position -= new Vector2(2, 0);

            if (state.IsKeyDown(Keys.E))
                manTrans.Scale += new Vector2(0.1f);

            if (state.IsKeyDown(Keys.Q))
                manTrans.Scale -= new Vector2(0.1f);

            if (state.IsKeyDown(Keys.W))
                manTrans.Rotation += 0.1f;

            if (state.IsKeyDown(Keys.S))
                manTrans.Rotation -= 0.1f;

            if (state.IsKeyDown(Keys.Z))
                manHatTrans.LocalRotation += 0.1f;

            if (state.IsKeyDown(Keys.X))
                manHatTrans.LocalRotation -= 0.1f;

            if (state.IsKeyDown(Keys.Right))
                manBackpackTrans.LocalPosition += new Vector2(2, 0);

            if (state.IsKeyDown(Keys.Left))
                manBackpackTrans.LocalPosition -= new Vector2(2, 0);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();

            _spriteBatch.Draw(texture, trans.Position, null, Color.White, trans.Rotation, new Vector2(1), trans.Scale, SpriteEffects.None, 0);
            _spriteBatch.Draw(texture, trans2.Position, null, Color.Red, trans2.Rotation, new Vector2(1), trans2.Scale, SpriteEffects.None, 0);

            _spriteBatch.Draw(manpng, manBackpackTrans.Position, new Rectangle(18 * 1, 0, 18, 37), Color.White, manBackpackTrans.Rotation, new Vector2(9, 18), manBackpackTrans.Scale, SpriteEffects.None, 0);
            _spriteBatch.Draw(manpng, manTrans.Position, new Rectangle(18 * 0, 0, 18, 37), Color.White, manTrans.Rotation, new Vector2(9, 18), manTrans.Scale, SpriteEffects.None, 0);
            _spriteBatch.Draw(manpng, manHatTrans.Position, new Rectangle(18 * 2, 0, 18, 37), Color.White, manHatTrans.Rotation, new Vector2(9, 18), manHatTrans.Scale, SpriteEffects.None, 0);

            _spriteBatch.End();
            // TODO: Add your drawing code here

            base.Draw(gameTime);
        }
    }
}
