// <copyright>
// Copyright by Central Christian Church
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;
namespace com.centralaz.Baptism.Migrations
{
    [MigrationNumber( 1, "1.0.14" )]
    public class CreateDb : Migration
    {
        public override void Up()
        {
            // Set up attendance group
            RockMigrationHelper.AddGroupType( "Attendance Group Type", "Group type used to record weekend attendance based off of wifi", "Group", "Member", false, false, false, "", 0, null, 0, null, "A81BEE5B-8BF5-4AB8-A8DF-CC0705C1A776" );
            Sql( @"
                Update [GroupType]
                Set [TakesAttendance] = 1,
                    [AttendanceCountsAsWeekendService] = 1
                Where [Guid] = 'A81BEE5B-8BF5-4AB8-A8DF-CC0705C1A776'" );
            RockMigrationHelper.UpdateGroup( null, "A81BEE5B-8BF5-4AB8-A8DF-CC0705C1A776", "Meraki Attendance", "Used to record weekend attendance", null, 0, "DA46E0EC-1183-40C5-BB1B-554D1100FAD9" );

            // Meraki Attendance Group
            RockMigrationHelper.AddGlobalAttribute( "F4399CEF-827B-48B2-A735-F7806FCFE8E8", "", "", "Meraki Attendance Group", "The group used to take attendance from Meraki routers", 101, "DA46E0EC-1183-40C5-BB1B-554D1100FAD9", "D3162F8F-C62F-4023-90FD-BCBE34824908" );

            // Meraki Secret
            RockMigrationHelper.AddGlobalAttribute( "9C204CD0-1233-41C5-818A-C5DA439445AA", "", "", "Meraki Secret", "The Secret code for Meraki server validation", 102, "", "0FD7B99C-9608-416E-929D-B158D62A6172" );

            // Mac address
            RockMigrationHelper.UpdatePersonAttribute( "9C204CD0-1233-41C5-818A-C5DA439445AA", null, "Mac Address", "MacAddress", "", "The Mac Address of the device the person uses", 100, "", "E4A05461-81EE-490C-8866-DAF5D86FDC3E" );
        }
        public override void Down()
        {

        }
    }
}
