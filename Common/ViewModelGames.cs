using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class ViewModelGames
    {
        
        public Snakes SnakesPlayers { get; set; }
        public Snakes.Point Points { get; set; }
        public int Top { get; set; }
        public int IdSnake { get; set; }
    }
}
