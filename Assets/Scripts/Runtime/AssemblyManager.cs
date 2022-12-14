using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace LD48
{
    public class AssemblyManager : MonoBehaviour
    {
        private const string key_rocket = "baseanimtile";
        private const string key_drill = "drillanimtile";
        private const string key_refinery = "factory_0";
        private const string key_smelter = "factory_armory"; // TODO umbennen
        private const string key_armory = "factory_smelter"; // TODO umbennen

        private const string key_conveyer_SW_NE = "conveyors_anim_swne";
        private const string key_conveyer_SW_NW = "conveyors_anim_swnw";
        private const string key_conveyer_SW_SE = "conveyors_anim_swse";
        private const string key_conveyer_SE_NW = "conveyors_anim_senw";
        private const string key_conveyer_SE_NE = "conveyors_anim_sene";
        private const string key_conveyer_SE_SW = "conveyors_anim_sesw";
        private const string key_conveyer_NW_SE = "conveyors_anim_nwse";
        private const string key_conveyer_NW_SW = "conveyors_anim_nwsw";
        private const string key_conveyer_NW_NE = "conveyors_anim_nwne";
        private const string key_conveyer_NE_SW = "conveyors_anim_nesw";
        private const string key_conveyer_NE_SE = "conveyors_anim_nese";
        private const string key_conveyer_NE_NW = "conveyors_anim_nenw";

        [Header("Static References")] public Tilemap tilemap;
        public Tilemap tilemapTerrain;
        public Tilemap tilemapBoxes;
        public Vector2Int minTilemapCoordinates;
        public Vector2Int maxTilemapCoordinates;

        [Space] public StringMachineInfoDictionary machinery;
        public StringTileDictionary resources;
        public List<string> ignoreTileTypes;
        public List<ResourceNodeSO> resourceNodes;

        [Header("Configuration")] public float tickSpeed = 0.2f;

        [Header("Runtime Info")] public AssemblyLine assembly;
        private AssemblyLine previousAssembly;
        private float nextTick;

        private void Start()
        {
            previousAssembly = new AssemblyLine();
            CreateModel();
        }

        private void Update()
        {
            if (Time.time > nextTick)
            {
                Tick();
                nextTick = Time.time + tickSpeed;
            }
        }

        public void CreateModel()
        {
            previousAssembly = assembly;
            assembly = new AssemblyLine();
            HashSet<string> machineTypes = new HashSet<string>();
            HashSet<string> unmappedMachineTypes = new HashSet<string>();

            for (int x = minTilemapCoordinates.x; x < maxTilemapCoordinates.x; x++)
            {
                for (int y = minTilemapCoordinates.y; y < maxTilemapCoordinates.y; y++)
                {
                    if (tilemap.HasTile(new Vector3Int(x, y, 0)))
                    {
                        TileBase tile = tilemap.GetTile(new Vector3Int(x, y, 0));

                        string key = tile.name.ToLowerInvariant();
                        machineTypes.Add(key);
                        if (!machinery.ContainsKey(key))
                        {
                            if (!ignoreTileTypes.Contains(key)) unmappedMachineTypes.Add(key);
                            continue;
                        }

                        Machine m = new Machine(machinery[key]);
                        m.position = new Vector3Int(x, y, 0);
                        assembly.WithMachine(m);

                        m.OnOutputProduced += (machine, packages) =>
                        {
                            if (m.info.productionEffect != null)
                            {
                                Vector3 pos = tilemap.GetCellCenterWorld(m.position) + new Vector3(0, 1, 0);
                                GameObject effect = Instantiate(m.info.productionEffect, pos, Quaternion.identity);
                                Destroy(effect, 4);
                            }
                        };
                    }
                }
            }

            MatchAllMachines();

            Debug.Log("Detected the following tile types: " + string.Join(", ", machineTypes));
            if (unmappedMachineTypes.Count > 0) Debug.LogError("Failed to map the following tile types: " + string.Join(", ", unmappedMachineTypes));
        }

        public void Tick()
        {
            assembly.Tick();

            UpdateBoxes();
        }

        public Machine GetRocket()
        {
            foreach (Machine m in assembly.machines)
            {
                if (m.info.key.Equals(key_rocket))
                {
                    return m;
                }
            }
            return null;
        }

        private void UpdateBoxes()
        {
            tilemapBoxes.ClearAllTiles();
            assembly.machines.ForEach(m =>
            {
                if (!m.info.key.Contains("conveyors")) return;

                m.outputStorage.ForEach(o =>
                {
                    if (!resources.ContainsKey(o.material))
                    {
                        Debug.LogError("Unassigned resource found: " + o.material);
                        return;
                    }
                    Vector3Int newPos = m.position;
                    tilemapBoxes.SetTile(newPos, resources[o.material]);
                    o.lastPosition = newPos;
                });
            });
        }

        private void MatchAllMachines()
        {
            foreach (Machine m in assembly.machines)
            {
                // TODO get name form scriptableObject?
                if (m.info.key.Equals(key_conveyer_SE_NW))
                {
                    SetBuildingInput(m, new Vector2Int(m.position.x, m.position.y - 1)); // get coming from machine
                    SetBuildingOutput(m, new Vector2Int(m.position.x, m.position.y + 1)); // get going to machine
                }
                else if (m.info.key.Equals(key_conveyer_SW_NE))
                {
                    SetBuildingInput(m, new Vector2Int(m.position.x - 1, m.position.y)); // get coming from machine
                    SetBuildingOutput(m, new Vector2Int(m.position.x + 1, m.position.y)); // get going to machine
                }
                else if (m.info.key.Equals(key_conveyer_NW_SE))
                {
                    SetBuildingInput(m, new Vector2Int(m.position.x, m.position.y + 1)); // get coming from machine
                    SetBuildingOutput(m, new Vector2Int(m.position.x, m.position.y - 1)); // get going to machine
                }
                else if (m.info.key.Equals(key_conveyer_NE_SW))
                {
                    SetBuildingInput(m, new Vector2Int(m.position.x + 1, m.position.y)); // get coming from machine
                    SetBuildingOutput(m, new Vector2Int(m.position.x - 1, m.position.y)); // get going to machine
                }

                if (m.info.key.Equals(key_conveyer_SE_NE))
                {
                    SetBuildingInput(m, new Vector2Int(m.position.x, m.position.y - 1)); // get coming from machine
                    SetBuildingOutput(m, new Vector2Int(m.position.x + 1, m.position.y)); // get going to machine
                }
                else if (m.info.key.Equals(key_conveyer_SE_SW))
                {
                    SetBuildingInput(m, new Vector2Int(m.position.x, m.position.y - 1)); // get coming from machine
                    SetBuildingOutput(m, new Vector2Int(m.position.x - 1, m.position.y)); // get going to machine
                }
                else if (m.info.key.Equals(key_conveyer_SW_SE))
                {
                    SetBuildingInput(m, new Vector2Int(m.position.x - 1, m.position.y)); // get coming from machine
                    SetBuildingOutput(m, new Vector2Int(m.position.x, m.position.y - 1)); // get going to machine
                }
                else if (m.info.key.Equals(key_conveyer_SW_NW))
                {
                    SetBuildingInput(m, new Vector2Int(m.position.x - 1, m.position.y)); // get coming from machine
                    SetBuildingOutput(m, new Vector2Int(m.position.x, m.position.y + 1)); // get going to machine
                }
                if (m.info.key.Equals(key_conveyer_NW_NE))
                {
                    SetBuildingInput(m, new Vector2Int(m.position.x, m.position.y + 1)); // get coming from machine
                    SetBuildingOutput(m, new Vector2Int(m.position.x + 1, m.position.y)); // get going to machine
                }
                else if (m.info.key.Equals(key_conveyer_NW_SW))
                {
                    SetBuildingInput(m, new Vector2Int(m.position.x, m.position.y + 1)); // get coming from machine
                    SetBuildingOutput(m, new Vector2Int(m.position.x - 1, m.position.y)); // get going to machine
                }
                else if (m.info.key.Equals(key_conveyer_NE_SE))
                {
                    SetBuildingInput(m, new Vector2Int(m.position.x + 1, m.position.y)); // get coming from machine
                    SetBuildingOutput(m, new Vector2Int(m.position.x, m.position.y - 1)); // get going to machine
                }
                else if (m.info.key.Equals(key_conveyer_NE_NW))
                {
                    SetBuildingInput(m, new Vector2Int(m.position.x + 1, m.position.y)); // get coming from machine
                    SetBuildingOutput(m, new Vector2Int(m.position.x, m.position.y + 1)); // get going to machine
                }

                if (m.info.key.Equals(key_drill))
                {
                    TileBase resTile = tilemapTerrain.GetTile(new Vector3Int(m.position.x, m.position.y, 0));

                    // check the resource node
                    foreach (ResourceNodeSO resNode in resourceNodes)
                    {
                        if (resNode.resourceNodeTiles.Contains(resTile))
                        {
                            m.info = resNode.machineInfo;
                        }
                    }
                }

                // check if machine was already in previous assembly and use its parameters
                Machine previousMachine = GetMachineAtPosition(previousAssembly, new Vector2Int(m.position.x, m.position.y));
                if (previousMachine != null && previousMachine.info.key.Equals(m.info.key))
                {
                    m.inputStorage = previousMachine.inputStorage;
                    m.outputStorage = previousMachine.outputStorage;
                    m.tempStorage = previousMachine.tempStorage;
                    m.info = previousMachine.info;

                    previousAssembly.machines.Remove(previousMachine);
                }
            }
        }

        private void SetBuildingInput(Machine parentMachine, Vector2Int searchPosition)
        {
            Machine neighbour = GetMachineAtPosition(searchPosition);

            if (neighbour != null)
            {
                if (!MatchNeighbourInput(neighbour, parentMachine))
                {
                    // if neighbour is not a big machine, that still at it as input
                    parentMachine.inputPorts.Add(new Port(neighbour));
                }
            }
            else
            {
                Vector2Int alternativeSearchPostion = new Vector2Int(searchPosition.x - 1, searchPosition.y);
                if (MatchNeighbourInput(GetMachineAtPosition(alternativeSearchPostion), parentMachine))
                {
                    return;
                }

                alternativeSearchPostion = new Vector2Int(searchPosition.x, searchPosition.y - 1);
                if (MatchNeighbourInput(GetMachineAtPosition(alternativeSearchPostion), parentMachine))
                {
                    return;
                }

                alternativeSearchPostion = new Vector2Int(searchPosition.x - 1, searchPosition.y - 1);
                if (MatchNeighbourInput(GetMachineAtPosition(alternativeSearchPostion), parentMachine))
                {
                    return;
                }
            }
        }

        private void SetBuildingOutput(Machine parentMachine, Vector2Int searchPosition)
        {
            Machine neighbour = GetMachineAtPosition(searchPosition);

            if (neighbour != null)
            {
                if (!MatchNeighbourOutput(neighbour, parentMachine))
                {
                    // if neighbour is not assembly big machine, than still set it as output
                    parentMachine.outputPorts.Add(new Port(neighbour));
                }
            }
            else
            {
                Vector2Int alternativeSearchPostion = new Vector2Int(searchPosition.x - 1, searchPosition.y);
                if (MatchNeighbourOutput(GetMachineAtPosition(alternativeSearchPostion), parentMachine))
                {
                    return;
                }

                alternativeSearchPostion = new Vector2Int(searchPosition.x, searchPosition.y - 1);
                if (MatchNeighbourOutput(GetMachineAtPosition(alternativeSearchPostion), parentMachine))
                {
                    return;
                }

                alternativeSearchPostion = new Vector2Int(searchPosition.x - 1, searchPosition.y - 1);
                if (MatchNeighbourOutput(GetMachineAtPosition(alternativeSearchPostion), parentMachine))
                {
                    return;
                }

                // check if output goes into the rocket
                if (searchPosition.x >= -1 && searchPosition.x <= 1 && searchPosition.y >= -1 && searchPosition.y <= 1)
                {
                    Vector2Int rocketPosition = new Vector2Int(0, 0);
                    Machine rocket = GetMachineAtPosition(rocketPosition);

                    if (rocket != null)
                    {
                        parentMachine.outputPorts.Add(new Port(rocket));
                        rocket.inputPorts.Add(new Port(parentMachine));
                    }
                    else
                    {
                        Debug.Log("Rocket not found at " + rocketPosition);
                    }
                }
            }
        }

        private bool MatchNeighbourInput(Machine neighbour, Machine parentMachine)
        {
            if (neighbour != null)
            {
                if (neighbour.info.key.Equals(key_drill) ||
                    neighbour.info.key.Equals(key_refinery) ||
                    neighbour.info.key.Equals(key_smelter) ||
                    neighbour.info.key.Equals(key_armory))
                {
                    parentMachine.inputPorts.Add(new Port(neighbour));
                    neighbour.outputPorts.Add(new Port(parentMachine));
                    return true;
                }
            }
            return false;
        }

        private bool MatchNeighbourOutput(Machine neighbour, Machine parentMachine)
        {
            if (neighbour != null)
            {
                if (neighbour.info.key.Equals(key_drill) ||
                    neighbour.info.key.Equals(key_refinery) ||
                    neighbour.info.key.Equals(key_smelter) ||
                    neighbour.info.key.Equals(key_armory))
                {
                    parentMachine.outputPorts.Add(new Port(neighbour));
                    neighbour.inputPorts.Add(new Port(parentMachine));
                    return true;
                }
            }
            return false;
        }

        private Machine GetMachineAtPosition(Vector2Int position)
        {
            foreach (Machine m in assembly.machines)
            {
                if (new Vector2Int(m.position.x, m.position.y).Equals(position)) return m;
            }

            return null;
        }

        private Machine GetMachineAtPosition(AssemblyLine assLine, Vector2Int position)
        {
            foreach (Machine m in assLine.machines)
            {
                if (new Vector2Int(m.position.x, m.position.y).Equals(position)) return m;
            }

            return null;
        }
    }
}