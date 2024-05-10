using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OneJump.src.ui;

namespace OneJump.src.engine {
    public class EntityBuilder {
        public List<Entity.EntityUpdate> updateHandlers = new();
        public List<Entity.EntityTexture> textureHandlers = new();
        public float width, height;
        public int flags;
        public string tag;
        public EntityBuilder() => EntityBuilders.Entities.Add(this);
        public EntityBuilder AddUpdateHandler(Entity.EntityUpdate handler) {
            updateHandlers.Add(handler);
            return this;
        }
        public EntityBuilder AddTextureHandler(Entity.EntityTexture handler) {
            textureHandlers.Add(handler);
            return this;
        }
        public EntityBuilder Size(float width, float height) {
            this.width = width;
            this.height = height;
            return this;
        }
        public EntityBuilder Flags(int flags) {
            this.flags |= flags;
            return this;
        }
        public EntityBuilder Tag(string tag) {
            this.tag = tag;
            return this;
        }
        public EntityBuilder LEHint_Property<T>(string name) => this;
        public EntityBuilder LEHint_Texture(string name) => this;
        public EntityBuilder LEHint_Hide() => this;
        public Entity Build() {
            Entity entity = new() {
                Width = width,
                Height = height,
                Flags = flags,
                Tag = tag
            };
            foreach (Entity.EntityUpdate handler in updateHandlers) {
                entity.UpdateHandlers += handler;
            }
            foreach (Entity.EntityTexture handler in textureHandlers) {
                entity.TextureHandlers += handler;
            }
            return entity;
        }
    }
    public class EntityBuilders {
        private static bool Init(Entity entity) {
            bool inited = entity.GetOrDefaultProperty("inited", false);
            entity.SetProperty("inited", true);
            return !inited;
        }
        public static Entity.EntityUpdate UH_Controller(
            float maxSpeed,
            float jump,
            float acc, float dec
        ) => (Entity entity) => {
            bool canJump = entity.GetOrDefaultProperty("can_jump", true);
            bool left = Input.ButtonDown(Input.MoveLeft);
            bool right = Input.ButtonDown(Input.MoveRight);
            float accel = acc, decel = dec;
            if (entity.InAir && !(left ^ right)) {
                accel = 0;
                decel = 0;
            }
            if (left && !right) {
                entity.SpeedX -= entity.SpeedX > 0 ? decel : accel;
                if (entity.SpeedX < -maxSpeed) entity.SpeedX = -maxSpeed;
            }
            else if (right && !left) {
                entity.SpeedX += entity.SpeedX < 0 ? decel : accel;
                if (entity.SpeedX > maxSpeed) entity.SpeedX = maxSpeed;
            }
            else {
                if (entity.SpeedX > 0) {
                    entity.SpeedX -= decel;
                    if (entity.SpeedX < 0) entity.SpeedX = 0;
                }
                if (entity.SpeedX < 0) {
                    entity.SpeedX += decel;
                    if (entity.SpeedX > 0) entity.SpeedX = 0;
                }
            }
            if (Input.ButtonPressed(Input.Jump) && (canJump || entity.HasProperty("infjump"))) {
                entity.Jump(jump);
                entity.SetProperty("can_jump", false);
                entity.PlaySound("sounds/jump.wav");
                if (!entity.HasProperty("infjump")) {
                    Sliders<Main, Color>.Add(null, "GameColor", new(0.25f, 1.00f, 0.25f, 1.00f), 30, Sliders.ColorInterpolator, Sliders.EasingCubicOut);
                    Sliders<Scene, float>.Add(Main.CurrentScene, "BackdropScale", 0.8f, 30, Sliders.FloatInterpolator, Sliders.EasingCubicOut);
                }
            }
            if (Input.ButtonPressed(Input.PickUp)) {
                if (entity.HasProperty("picked_up_cube")) {
                    Entity cube = entity.GetProperty<Entity>("picked_up_cube");
                    cube.SetProperty("picked_up", false);
                    cube.X += (entity.Width / 2 + cube.Width / 2 + .01f) * (entity.FlipX ? -1 : 1) + entity.SpeedX;
                    entity.RemoveProperty("picked_up_cube");
                    entity.Height -= cube.Height;
                    entity.PlaySound("sounds/drop.wav");
                }
                else {
                    (Entity cube, float dist) = Main.CurrentScene.NearestEntityWithTag(entity, "cube");
                    if (dist <= 0.85) {
                        cube.SetProperty("picked_up", true);
                        entity.SetProperty("picked_up_cube", cube);
                        entity.Height += cube.Height;
                        entity.PlaySound("sounds/pickup.wav");
                    }
                }
            }
            if (Input.ButtonPressed(Input.InfJump)) {
                entity.SetProperty("infjump", true);
                Sliders<Main, Color>.Add(null, "GameColor", new(0.0f, 1.0f, 0.0f, 1.0f), 30, Sliders.ColorInterpolator, Sliders.EasingCubicOut);
                Sliders<Scene, float>.Add(Main.CurrentScene, "BackdropScale", 1f, 30, Sliders.FloatInterpolator, Sliders.EasingCubicOut);
            }
            if (Input.ButtonPressed(Input.Reset)) Main.CurrentScene.Die(entity, "reset");
            if (Input.ButtonPressed(Input.Pause) && !Main.IrisActive) {
                Main.Paused = true;
                Sliders<Scene, float>.Add(Main.CurrentScene, "Scale", Scene.GAME_SCALE * 0.5f, 30, Sliders.FloatInterpolator, Sliders.EasingCubicInOut);
                Sliders<Scene, float>.Add(Main.CurrentScene, "BackdropScale", 8, 30, Sliders.FloatInterpolator, Sliders.EasingCubicInOut);
                Main.UI = MenuLoader.LoadPauseMenu();
                SFXPlayer.Play("sounds/pause.wav");
                Main.Music.Pause();
            }
            if (entity.Y >= Main.CurrentScene.Tilemap.height + 3) Main.CurrentScene.Die(entity, "huh???");
        };
        public static Entity.EntityUpdate UH_Gravity(
            float gravity, float terminalVelocitry
        ) => (Entity entity) => {
            entity.SpeedY += gravity;
            if (entity.SpeedY > terminalVelocitry) entity.SpeedY = terminalVelocitry;
        };
        public static Entity.EntityUpdate UH_Logic(
            int numInputs, int numOutputs, Func<bool[], bool[]> func
        ) => (Entity entity) => {
            bool[] inputs = new bool[numInputs];
            for (int i = 0; i < numInputs; i++) {
                inputs[i] = Main.CurrentScene.events[entity.GetProperty<byte>("event_in" + (i + 1))];
            }
            bool[] outputs = func(inputs);
            for (int i = 0; i < numOutputs; i++) {
                Main.CurrentScene.events[entity.GetProperty<byte>("event_out" + (i + 1))] = outputs[i];
            }
        };
        public static Entity.EntityUpdate UH_Button() => (Entity entity) => {
            bool prev = Main.CurrentScene.events[entity.GetProperty<byte>("target")], curr;
            Main.CurrentScene.events[entity.GetProperty<byte>("target")] = curr = entity.GetColliders().Length != 0;
            if (!prev && curr) entity.PlaySound("sounds/on.wav");
            if (prev && !curr) entity.PlaySound("sounds/off.wav");
        };
        public static Entity.EntityUpdate UH_Cube() => (Entity entity) => {
            if (!entity.GetProperty<bool>("picked_up")) {
                entity.Flags |=  Entity.FlagSolidHitbox;
                entity.Flags &= ~Entity.FlagDisableCollision;
                return;
            }
            (Entity player, float dist) = Main.CurrentScene.NearestEntityWithTag(entity, "player");
            if (player == null) (player, dist) = Main.CurrentScene.NearestEntityWithTag(entity, "player_finish");
            if (player == null) {
                entity.SetProperty("picked_up", false);
                return;
            }
            entity.X = player.X;
            entity.Y = player.Y - player.Height + entity.Height;
            entity.SpeedX = 0;
            entity.SpeedY = 0;
            entity.Flags &= ~Entity.FlagSolidHitbox;
            entity.Flags |=  Entity.FlagDisableCollision;
        };
        public static Entity.EntityUpdate UH_Key() => (Entity entity) => {
            Entity[] touchingEntities = entity.GetColliders();
            foreach (Entity e in touchingEntities) {
                if (e.Tag == "player") {
                    if (e.HasProperty("infjump")) Main.CurrentScene.Die(e, "cheated");
                    else {
                        e.Tag = "player_finish";
                        entity.PlaySound("sounds/finish.wav");
                        entity.Despawn();
                        Main.CurrentScene.SpawnDeathParticles(entity.X, entity.Y - entity.Height / 2);
                        Sliders<Scene, float>.Add(Main.CurrentScene, "BackdropScale", 1.25f, 20, Sliders.FloatInterpolator, Sliders.EasingCubicOut);
                        Sliders<Main, Color>.Add(null, "GameColor", new(0f, .8f, 0f, 1f), 20, Sliders.ColorInterpolator, Sliders.EasingCubicIn);
                        Main.Iris(entity.X, entity.Y - entity.Height / 2, "level complete", 30, true, () => {
                            Main.CurrentScene.events[entity.GetProperty<byte>("target")] = true;
                        });
                    }
                }
            }
        };
        public static Entity.EntityUpdate UH_LevelSwitch() => (Entity entity) => {
            if (Main.CurrentScene.events[entity.GetProperty<byte>("trigger")]) {
                Main.CurrentScene = Assets.GetAsset<Scene>("levels/level" + entity.GetProperty<byte>("lvlid") + ".lvl");
                Main.CurrentScene.Reload();
            }
        };
        public static Entity.EntityUpdate UH_Door(
            bool vertical
        ) => (Entity entity) => {
            entity.AddProperty("homeX", entity.X);
            entity.AddProperty("homeY", entity.Y);
            int value = entity.GetOrDefaultProperty("open", 0);
            if (Main.CurrentScene.events[entity.GetProperty<byte>("trigger")]) {
                value++;
                if (value > 8) value = 8;
            }
            else {
                value--;
                if (value < 0) value = 0;
            }
            entity.SetProperty("open", value);
            if (vertical) entity.Y = entity.GetProperty<float>("homeY") - value / 8.0f;
            else          entity.X = entity.GetProperty<float>("homeX") - value / 8.0f;
        };
        public static Entity.EntityUpdate UH_Platform() => (Entity entity) => {
            bool enabled = Main.CurrentScene.events[entity.GetProperty<byte>("trigger")];
            if (entity.GetProperty<byte>("enabled") != 0) enabled ^= true;
            entity.Y += 0.75f;
            (Entity node, float dist) = Main.CurrentScene.NearestEntityWithCondition(entity, (Entity e) => e.Tag == "pathnode" && e.GetProperty<byte>("id") == entity.GetProperty<byte>("node"));
            if (Init(entity)) {
                entity.X = node.X;
                entity.Y = node.Y;
                entity.SetProperty("next_node", node.GetProperty<byte>("next"));
                entity.SetProperty("speed", .0f);
                entity.SetProperty("timer", node.GetProperty<int>("stay_time"));
            }
            if (!enabled) {
                entity.Y -= 0.75f;
                return;
            }
            (Entity nextNode, float nextDist) = Main.CurrentScene.NearestEntityWithCondition(entity, (Entity e) => e.Tag == "pathnode" && e.GetProperty<byte>("id") == entity.GetProperty<byte>("next_node"));
            int timer = entity.GetProperty<int>("timer");
            if (timer-- == 0) {
                float speed = entity.GetProperty<float>("speed");
                if (speed == 0) {
                    timer = node.GetProperty<int>("next_move_time");
                    speed = nextDist / timer;
                }
                else {
                    speed = 0;
                    entity.SetProperty("node", entity.GetProperty<byte>("next_node"));
                    entity.SetProperty("inited", false);
                }
                entity.SetProperty("speed", speed);
            }
            if (nextNode == null) return;
            double angle = Math.Atan2(nextNode.Y - entity.Y, nextNode.X - entity.X);
            entity.X += (float)Math.Cos(angle) * entity.GetProperty<float>("speed");
            entity.Y += (float)Math.Sin(angle) * entity.GetProperty<float>("speed");
            entity.SetProperty("timer", timer);
            entity.Y -= 0.75f;
        };
        public static Entity.EntityUpdate UH_DeathParticle() => (Entity entity) => {
            if (Init(entity)) {
                float radius = Random.Shared.NextSingle() * .3f;
                float angle = (float)(Random.Shared.NextSingle() * 2 * Math.PI);
                float x = (float)(Math.Cos(angle) * radius);
                float y = (float)(Math.Sin(angle) * radius);
                entity.SpeedX = x;
                entity.SpeedY = y - 0.25f;
            }
        };

        public static Entity.EntityTexture TH_Static(
            string path
        ) => (Entity entity) => {
            return Assets.GetAsset<Texture2D>(path);
        };
        public static Entity.EntityTexture TH_Player(
            string stationary, string walking, string jumping
        ) => (Entity entity) => {
            string tex = "";
            if (entity.SpeedX < 0) entity.FlipX = true;
            if (entity.SpeedX > 0) entity.FlipX = false;
            if (entity.SpeedX == 0) tex = stationary;
            else tex = Main.GlobalTimer % 20 < 10 ? stationary : walking;
            if (entity.InAir) tex = jumping;
            return Assets.GetAsset<Texture2D>(tex);
        };
        public static Entity.EntityTexture TH_Button(
            string idle, string pressed
        ) => (Entity entity) => {
            if (entity.GetColliders().Length == 0) return Assets.GetAsset<Texture2D>(idle);
            return Assets.GetAsset<Texture2D>(pressed);
        };
        public static readonly List<EntityBuilder> Entities = new();
        // LE_EntityBegin
        public static readonly EntityBuilder Player = new EntityBuilder()
            .Size(0.8f, 1.0f)
            .Flags(Entity.FlagAlternateCollCorr)
            .Tag("player")
            .AddUpdateHandler(UH_Controller(0.15f, 0.5f, 0.01f, 0.025f))
            .AddUpdateHandler(UH_Gravity(0.03f, 1f))
            .AddTextureHandler(TH_Player("images/objects/player_standing.png", "images/objects/player_walking.png", "images/objects/player_jumping.png"))
            .LEHint_Texture("images/objects/player_standing.png");
        public static readonly EntityBuilder LogicFork = new EntityBuilder()
            .AddUpdateHandler(UH_Logic(1, 2, (bool[] input) => new bool[]{ input[0], input[0] }))
            .LEHint_Property<byte>("event_in1")
            .LEHint_Property<byte>("event_out1")
            .LEHint_Property<byte>("event_out2")
            .LEHint_Texture("images/editor/fork.png");
        public static readonly EntityBuilder LogicAnd = new EntityBuilder()
            .AddUpdateHandler(UH_Logic(2, 1, (bool[] input) => new bool[]{ input[0] && input[1] }))
            .LEHint_Property<byte>("event_in1")
            .LEHint_Property<byte>("event_in2")
            .LEHint_Property<byte>("event_out1")
            .LEHint_Texture("images/editor/and.png");
        public static readonly EntityBuilder LogicOr = new EntityBuilder()
            .AddUpdateHandler(UH_Logic(2, 1, (bool[] input) => new bool[]{ input[0] || input[1] }))
            .LEHint_Property<byte>("event_in1")
            .LEHint_Property<byte>("event_in2")
            .LEHint_Property<byte>("event_out1")
            .LEHint_Texture("images/editor/or.png");
        public static readonly EntityBuilder LogicNot = new EntityBuilder()
            .AddUpdateHandler(UH_Logic(1, 1, (bool[] input) => new bool[]{ !input[0] }))
            .LEHint_Property<byte>("event_in1")
            .LEHint_Property<byte>("event_out1")
            .LEHint_Texture("images/editor/not.png");
        public static readonly EntityBuilder LogicFire = new EntityBuilder()
            .Flags(Entity.FlagShouldDelete)
            .AddUpdateHandler(UH_Logic(0, 1, (bool[] input) => new bool[]{ true }))
            .LEHint_Property<byte>("event_out1")
            .LEHint_Texture("images/editor/fire.png");
        public static readonly EntityBuilder Button = new EntityBuilder()
            .Size(1.0f, 0.125f)
            .Flags(Entity.FlagDisableCollision)
            .AddUpdateHandler(UH_Button())
            .AddTextureHandler(TH_Button("images/objects/button.png", "images/objects/button_pressed.png"))
            .LEHint_Property<byte>("target")
            .LEHint_Texture("images/objects/button.png");
        public static readonly EntityBuilder Cube = new EntityBuilder()
            .Size(0.75f, 0.75f)
            .Flags(Entity.FlagSolidHitbox)
            .Tag("cube")
            .AddUpdateHandler(UH_Gravity(0.02f, 0.8f))
            .AddUpdateHandler(UH_Cube())
            .AddTextureHandler(TH_Static("images/objects/cube.png"))
            .LEHint_Texture("images/objects/cube.png");
        public static readonly EntityBuilder Key = new EntityBuilder()
            .Size(1f, 1f)
            .Flags(Entity.FlagDisableCollision)
            .AddUpdateHandler(UH_Key())
            .AddTextureHandler(TH_Static("images/objects/key.png"))
            .LEHint_Property<byte>("target")
            .LEHint_Texture("images/objects/key.png");
        public static readonly EntityBuilder LevelSwitch = new EntityBuilder()
            .AddUpdateHandler(UH_LevelSwitch())
            .LEHint_Property<byte>("trigger")
            .LEHint_Property<byte>("lvlid")
            .LEHint_Texture("images/editor/lvlswitch.png");
        public static readonly EntityBuilder DoorVertical = new EntityBuilder()
            .Size(0.25f, 1.0f)
            .Flags(Entity.FlagSolidHitbox | Entity.FlagDisableCollision)
            .AddUpdateHandler(UH_Door(true))
            .AddTextureHandler(TH_Static("images/objects/door.png"))
            .LEHint_Property<byte>("trigger")
            .LEHint_Texture("images/objects/door.png");
        public static readonly EntityBuilder DoorHorizontal = new EntityBuilder()
            .Size(1.0f, 0.25f)
            .Flags(Entity.FlagSolidHitbox | Entity.FlagDisableCollision)
            .AddUpdateHandler(UH_Door(false))
            .AddTextureHandler(TH_Static("images/objects/horizontal_door.png"))
            .LEHint_Property<byte>("trigger")
            .LEHint_Texture("images/objects/horizontal_door.png");
        public static readonly EntityBuilder PathNode = new EntityBuilder()
            .Flags(Entity.FlagDisableCollision)
            .Tag("pathnode")
            .LEHint_Property<byte>("id")
            .LEHint_Property<byte>("next")
            .LEHint_Property<int>("next_move_time")
            .LEHint_Property<int>("stay_time")
            .LEHint_Texture("images/editor/pathnode.png");
        public static readonly EntityBuilder Platform = new EntityBuilder()
            .Size(1.0f, 0.25f)
            .Flags(Entity.FlagSolidHitbox | Entity.FlagDisableCollision)
            .AddUpdateHandler(UH_Platform())
            .AddTextureHandler(TH_Static("images/objects/horizontal_door.png"))
            .LEHint_Property<byte>("trigger")
            .LEHint_Property<byte>("enabled")
            .LEHint_Property<byte>("node")
            .LEHint_Texture("images/objects/horizontal_door.png");
        public static readonly EntityBuilder LogicXor = new EntityBuilder()
            .AddUpdateHandler(UH_Logic(2, 1, (bool[] input) => new bool[]{ input[0] ^ input[1] }))
            .LEHint_Property<byte>("event_in1")
            .LEHint_Property<byte>("event_in2")
            .LEHint_Property<byte>("event_out1")
            .LEHint_Texture("images/editor/xor.png");
        // LE_EntityEnd
        public static readonly EntityBuilder DeathParticle = new EntityBuilder()
            .Flags(Entity.FlagDisableCollision)
            .AddUpdateHandler(UH_Gravity(0.02f, 0.8f))
            .AddUpdateHandler(UH_DeathParticle())
            .AddTextureHandler(TH_Static("images/objects/death_particle.png"));
    }
}