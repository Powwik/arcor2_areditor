using System.Collections;
using System.Collections.Generic;
using Base;
using TriLibCore.General;

/*********************************************************************
 * \file RobotRangeStorage.cs
 * \the script stores the distances from the robot base for the range object
 * 
 * \author Daniel Zmrzlý
 *********************************************************************/
public class RobotRangeStorage : Singleton<RobotRangeStorage>
{
    public OrderedDictionary<int, float> DegreesValues = new();
    public Dictionary<string, OrderedDictionary<int, float>> RobotsRange = new Dictionary<string, OrderedDictionary<int, float>>();
    public List<IO.Swagger.Model.ListProjectsResponseData> Projects = new List<IO.Swagger.Model.ListProjectsResponseData>();

    /**
     * Function to insert robot ranges to the storage
     * 
     * \param[in] robot      type of the robot
     * \param[in] values      ranges of values
     */
    public void InsertRobotRanges(string robot, OrderedDictionary<int, float> values)
    {
        if (!RobotsRange.ContainsKey(robot))
        {
            RobotsRange.Add(robot, values);
        }
    }
}
