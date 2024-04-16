using System.Collections;
using System.Collections.Generic;
using Base;
using TriLibCore.General;

public class RobotRangeStorage : Singleton<RobotRangeStorage>
{
    public OrderedDictionary<int, float> DegreesValues = new();
    public Dictionary<string, OrderedDictionary<int, float>> RobotsRange = new Dictionary<string, OrderedDictionary<int, float>>();
    public List<IO.Swagger.Model.ListProjectsResponseData> Projects = new List<IO.Swagger.Model.ListProjectsResponseData>();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void InsertRobotRanges(string robot, OrderedDictionary<int, float> values)
    {
        if (!RobotsRange.ContainsKey(robot))
        {
            RobotsRange.Add(robot, values);
        }
    }
}
