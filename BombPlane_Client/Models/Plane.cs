using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace BombPlane_Client.Models
{
    public class Coord_Point
    {
        public int x;
        public int y;
        public Coord_Point(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

    }

    public class Plane
    {
        public Coord_Point[][] plane_shape = new Coord_Point[4][];

        public enum plane_direction
        {
            up = 0,
            right =1,
            down  =2,
            left = 3
        }

        public Coord_Point center = new Coord_Point(0,0);
        public plane_direction Direction = plane_direction.up;

        public void Move_Plane(int x, int y)
        {
            center.x = x;
            center.y = y;
        }

        public void Rotate_Plane()
        {
            Direction = Direction + 1;
            if (Direction > plane_direction.left)
                Direction = plane_direction.up;
        }

        public bool Is_Outbound(int block_num)
        {
            if(this.Direction == plane_direction.up)
            {
                if (this.center.y - 1 < 0 || this.center.y + 2 > block_num || this.center.x - 2 < 0 || this.center.x + 2 > block_num)
                    return true;
                else
                    return false;
            }
            else if (this.Direction == plane_direction.left)
            {
                if (this.center.x - 1 < 0 || this.center.x + 2 > block_num || this.center.y - 2 < 0 || this.center.y + 2 > block_num)
                    return true;
                else
                    return false;
            }
            else if (this.Direction == plane_direction.right)
            {
                if (this.center.x - 2 < 0 || this.center.x + 1 > block_num || this.center.y - 2 < 0 || this.center.y + 2 > block_num)
                    return true;
                else
                    return false;
            }
            else
            {
                if (this.center.y - 2 < 0 || this.center.y + 1 > block_num || this.center.x - 2 < 0 || this.center.x + 2 > block_num)
                    return true;
                else
                    return false;
            }
        }
        public Plane()
        {
            // up
            plane_shape[0] = new Coord_Point[10];
            plane_shape[0][0] = new Coord_Point(0, -1);
            plane_shape[0][1] = new Coord_Point(-2, 0);
            plane_shape[0][2] = new Coord_Point(2, 0);
            plane_shape[0][3] = new Coord_Point(0, 0);
            plane_shape[0][4] = new Coord_Point(-1, 0);
            plane_shape[0][5] = new Coord_Point(1, 0);
            plane_shape[0][6] = new Coord_Point(0, 1);
            plane_shape[0][7] = new Coord_Point(0, 2);
            plane_shape[0][8] = new Coord_Point(-1, 2);
            plane_shape[0][9] = new Coord_Point(1, 2);

            // right
            plane_shape[1] = new Coord_Point[10];
            plane_shape[1][0] = new Coord_Point(1, 0);
            plane_shape[1][1] = new Coord_Point(0, -2);
            plane_shape[1][2] = new Coord_Point(0, 2);
            plane_shape[1][3] = new Coord_Point(0, -1);
            plane_shape[1][4] = new Coord_Point(0, 0);
            plane_shape[1][5] = new Coord_Point(0, 1);
            plane_shape[1][6] = new Coord_Point(-1, 0);
            plane_shape[1][7] = new Coord_Point(-2, -1);
            plane_shape[1][8] = new Coord_Point(-2, 0);
            plane_shape[1][9] = new Coord_Point(-2, 1);

            // down
            plane_shape[2] = new Coord_Point[10];
            plane_shape[2][0] = new Coord_Point(0, 1);
            plane_shape[2][1] = new Coord_Point(-2, 0);
            plane_shape[2][2] = new Coord_Point(2, 0);
            plane_shape[2][3] = new Coord_Point(0, 0);
            plane_shape[2][4] = new Coord_Point(-1, 0);
            plane_shape[2][5] = new Coord_Point(1, 0);
            plane_shape[2][6] = new Coord_Point(0, -1);
            plane_shape[2][7] = new Coord_Point(0, -2);
            plane_shape[2][8] = new Coord_Point(-1, -2);
            plane_shape[2][9] = new Coord_Point(1, -2);


            // left
            plane_shape[3] = new Coord_Point[10];
            plane_shape[3][0] = new Coord_Point(-1, 0);
            plane_shape[3][1] = new Coord_Point(0, -2);
            plane_shape[3][2] = new Coord_Point(0, 2);
            plane_shape[3][3] = new Coord_Point(0, -1);
            plane_shape[3][4] = new Coord_Point(0, 0);
            plane_shape[3][5] = new Coord_Point(0, 1);
            plane_shape[3][6] = new Coord_Point(1, 0);
            plane_shape[3][7] = new Coord_Point(2, -1);
            plane_shape[3][8] = new Coord_Point(2, 0);
            plane_shape[3][9] = new Coord_Point(2, 1);
        }
    }
}
