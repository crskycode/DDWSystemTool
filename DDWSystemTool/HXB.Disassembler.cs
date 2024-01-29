using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DDWSystemTool.HXB
{
    class Disassembler
    {
        readonly Stream _stream;
        readonly BinaryReader _reader;

        public StreamWriter TextOutput { get; set; }
        public Action<EValue, EValue[]> OnCallScript { get; set; }

        public bool IsUnicode { get => _unicode; }
        public Assembly Assembly { get => _assembly; }

        public Disassembler(byte[] input)
        {
            _stream = new MemoryStream(input);
            _reader = new BinaryReader(_stream);
        }

        bool _unicode;
        bool _debug;
        string[] _func;
        Assembly _assembly;

        public void Execute()
        {
            var header = _reader.ReadBytes(16);

            _unicode = header[2] == 'W' && header[3] == 'u';
            _debug = header[11] != 0;

            if (!_unicode || _debug)
            {
                throw new NotImplementedException();
            }

            _assembly = new Assembly();

            Init();

            while (_stream.Position < _stream.Length)
            {
                var address = Convert.ToInt32(_stream.Position);
                var code = _reader.ReadByte();

                if (TextOutput != null)
                {
                    if (_func[code] != null)
                        Text(address, ">", _func[code]);
                    else
                        Text(address, ">", $"func_{code:x4}");
                }

                switch (code)
                {
                    case 0x00: // eval
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        break;
                    }
                    case 0x01: // create_buffer
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // size
                        break;
                    }
                    case 0x02: // call
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        ExprList(); // parameters
                        DAddr(); // target
                        // jmp
                        break;
                    }
                    case 0x03: // call_script
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        var id = Expr(); // object index
                        var args = ExprList(); // parameters
                        OnCallScript?.Invoke(id, args);
                        break;
                    }
                    case 0x04:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // object index
                        break;
                    }
                    case 0x05:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        break;
                    }
                    case 0x06: // call_script_file
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // path
                        Expr(); // entry index
                        Expr(); // flags
                        ExprList(); // parameters
                        break;
                    }
                    case 0x07:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // -1, object index
                        break;
                    }
                    case 0x08: // set_clipboard_text
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // string
                        break;
                    }
                    case 0x09: // get_sound_status
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // channel index
                        break;
                    }
                    case 0x0A: // get_video_status
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        break;
                    }
                    case 0x0B: // set_imm_window_open
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // state
                        break;
                    }
                    case 0x0C:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr();
                        break;
                    }
                    case 0x0D: // copy_buffer
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index, destination
                        Expr(); // object index, source
                        Expr(); // size
                        break;
                    }
                    case 0x0E:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x0F:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr();
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x10: // create_font
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // name
                        Expr(); // size
                        Expr(); // weight
                        Expr();
                        Expr();
                        Expr();
                        //-------------------------
                        // if version >= 1210
                        Expr();
                        Expr();
                        //-------------------------
                        break;
                    }
                    case 0x11:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // path, text file
                        Expr(); // entry index
                        Expr(); // path2, image file
                        Expr(); // entry2 index
                        break;
                    }
                    case 0x12:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x13: // dlg_action
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // action, 0,3,4=MessageBox, 1=OutputDebugString, 2,6=SaveFileDialog
                        Expr(); // string
                        break;
                    }
                    case 0x14:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // object index
                        Expr();
                        break;
                    }
                    case 0x15:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr(); // object index
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x16:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr(); // object index
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x17:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x18:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x19: // end_script
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // code
                        break;
                    }
                    case 0x1A: // get_font_list
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        break;
                    }
                    case 0x1B: // free_object
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        break;
                    }
                    case 0x1C: // buf_read_byte
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // offset
                        break;
                    }
                    case 0x1D:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        break;
                    }
                    case 0x1E:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        break;
                    }
                    case 0x1F:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x20:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        break;
                    }
                    case 0x21:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        break;
                    }
                    case 0x22: // get_local_time
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        break;
                    }
                    case 0x23: // get_os_info
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        break;
                    }
                    case 0x24: // get_special_folder
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        break;
                    }
                    case 0x25: // get_time
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        break;
                    }
                    case 0x26: // jmp_if_true
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // bool
                        DAddr();
                        // jmp
                        break;
                    }
                    case 0x27: // jmp_if_false
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // bool
                        DAddr();
                        // jmp
                        break;
                    }
                    case 0x28: // jmp_boolean
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // bool
                        DAddr(); // target, true
                        DAddr(); // target, false
                        // jmp
                        break;
                    }
                    case 0x29: // jmp
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        DAddr();
                        // jmp
                        break;
                    }
                    case 0x2A: // jmp_switch
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // case index
                        int count = DWord();
                        for (int i = 0; i < count; i++)
                            DAddr();
                        // jmp
                        break;
                    }
                    case 0x2B: // load_file
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // path
                        break;
                    }
                    case 0x2C: // load_object
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // path
                        Expr(); // entry index
                        Expr();
                        break;
                    }
                    case 0x2D: // load_sound
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // path
                        Expr(); // entry index
                        Expr();
                        break;
                    }
                    case 0x2E:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // path
                        Expr(); // entry index
                        break;
                    }
                    case 0x2F: // set_window_pos
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // x
                        Expr(); // y
                        break;
                    }
                    case 0x30:
                    case 0x63:
                    case 0x67:
                    case 0x68:
                    case 0x69:
                    case 0x6B:
                    case 0x6F:
                    case 0x70:
                    {
                        _assembly.Add(address, 1, Instruction.Nop);
                        break;
                    }
                    case 0x31: // play_sound
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // channel
                        Expr();
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x32: // play_movie
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // path
                        Expr(); // x
                        Expr(); // y
                        Expr(); // width
                        Expr(); // height
                        Expr(); // volume
                        break;
                    }
                    case 0x33:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        break;
                    }
                    case 0x34:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        break;
                    }
                    case 0x35:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x36:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        DByte(); // variable type for eval
                        Expr();
                        break;
                    }
                    case 0x37: // reg_query_value
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // name
                        break;
                    }
                    case 0x38:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr();
                        break;
                    }
                    case 0x39:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        break;
                    }
                    case 0x3A:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x3B:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x3C: // ret
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // value
                        break;
                    }
                    case 0x3D:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // path
                        break;
                    }
                    case 0x3E: // dump_object
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // path
                        ExprList();
                        break;
                    }
                    case 0x3F: // get_input_state
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        break;
                    }
                    case 0x40: // set_full_screen
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // state
                        break;
                    }
                    case 0x41: // get_drive_path
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // volume name
                        Expr(); // only cd-rom, bool
                        break;
                    }
                    case 0x42: // reg_set_path
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // company
                        Expr(); // product
                        break;
                    }
                    case 0x43:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr();
                        break;
                    }
                    case 0x44: // buf_write_byte
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // offset
                        Expr(); // value
                        break;
                    }
                    case 0x45: // copy_string
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // offset
                        Expr(); // source pointer
                        break;
                    }
                    case 0x46:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // action
                        Expr(); // parameter
                        break;
                    }
                    case 0x47: // imm_set_composition_window_pos
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // x
                        Expr(); // y
                        break;
                    }
                    case 0x48:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // object index
                        Expr(); // object index
                        Expr();
                        break;
                    }
                    case 0x49: // set_cursor_pos
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // x
                        Expr(); // y
                        break;
                    }
                    case 0x4A:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr();
                        break;
                    }
                    case 0x4B: // snd_set_volume
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // volume
                        Expr();
                        break;
                    }
                    case 0x4C: // clear_global
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // index
                        Expr(); // size
                        Expr(); // value
                        break;
                    }
                    case 0x4D: // shell_execute
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // operation, "open", ...
                        Expr(); // file path
                        Expr(); // directory
                        break;
                    }
                    case 0x4E: // show_window
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // state
                        break;
                    }
                    case 0x4F: // sleep
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // time
                        break;
                    }
                    case 0x50: // stop_sound
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // channel
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x51: // stop_movie
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        break;
                    }
                    case 0x52: // strcat_g
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // string index
                        Expr(); // source
                        break;
                    }
                    case 0x53: // strcmp
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // str1
                        Expr(); // str2
                        break;
                    }
                    case 0x54: // set_glb_string
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // index
                        Expr(); // value
                        break;
                    }
                    case 0x55: // str_find_ch
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // string
                        Expr(); // wchar
                        Expr(); // start
                        Expr(); // direction, bool
                        break;
                    }
                    case 0x56: // eval_str_expr
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // string index
                        Expr(); // expr
                        break;
                    }
                    case 0x57: // strlen
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // string
                        break;
                    }
                    case 0x58:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x59:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // string index
                        Expr();
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x5A: // get_text_width
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index, font
                        Expr(); // string
                        break;
                    }
                    case 0x5B:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x5C:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        break;
                    }
                    case 0x5D:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        break;
                    }
                    case 0x5E: // reg_write
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // name
                        DByte(); // data type
                        Expr(); // value
                        break;
                    }
                    case 0x5F:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // offset
                        Expr();
                        break;
                    }
                    case 0x60:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x61: // load_dll
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // path
                        break;
                    }
                    case 0x62: // set_game_title
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // string
                        break;
                    }
                    case 0x64: // delete_file
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // path
                        Expr();
                        break;
                    }
                    case 0x65:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // action
                        Expr(); // parameter
                        break;
                    }
                    case 0x66: // load_image
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr(); // path
                        Expr(); // entry index
                        Expr();
                        break;
                    }
                    case 0x6A:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        ExprList();
                        break;
                    }
                    case 0x6C:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x6D: // create_edit_window
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x6E: // snd_set_volume
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // channel
                        Expr(); // volume
                        break;
                    }
                    case 0x71:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        Expr();
                        Expr();
                        DByte(); // data type
                        Expr();
                        break;
                    }
                    case 0x72: // show_dialog
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        break;
                    }
                    case 0x73: // show_menu
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // x
                        Expr(); // y
                        ExprList(); // items
                        break;
                    }
                    case 0x74: // convert_str_case
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // case, bool
                        Expr(); // string
                        break;
                    }
                    case 0x75:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // object index
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x76:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x77:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x78:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x79:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x7A:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x7B: // copy_file
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr(); // source path
                        Expr(); // destination path
                        break;
                    }
                    case 0x7C:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        Expr();
                        Expr();
                        Expr();
                        Expr();
                        break;
                    }
                    case 0x7D:
                    {
                        _assembly.Add(address, 1, Instruction.Func);
                        break;
                    }
                    case 0xFF: // End Of Script
                    {
                        _assembly.Add(address, 1, Instruction.End);
                        break;
                    }
                    default:
                    {
                        throw new Exception($"Unknown function instruction {code:X2}");
                    }
                }
            }

            if (_stream.Position != _stream.Length)
            {
                throw new Exception("Disassemble failed");
            }

            if (_assembly.BytesLength != _stream.Length - 16)
            {
                throw new Exception("Disassemble failed");
            }
        }

        void Init()
        {
            _func = new string[256];

            _func[0x00] = "expr";
            _func[0x01] = "create_buffer";
            _func[0x02] = "call";
            _func[0x03] = "call_script";
            _func[0x06] = "call_script_file";
            _func[0x08] = "set_clipboard_text";
            _func[0x09] = "get_sound_status";
            _func[0x0A] = "get_video_status";
            _func[0x0B] = "set_imm_window_open";
            _func[0x0D] = "copy_buffer";
            _func[0x10] = "create_font";
            _func[0x13] = "dlg_action";
            _func[0x19] = "end_script";
            _func[0x1A] = "get_font_list";
            _func[0x1B] = "free_object";
            _func[0x1C] = "buf_read_byte";
            _func[0x22] = "get_local_time";
            _func[0x23] = "get_os_info";
            _func[0x24] = "get_special_folder";
            _func[0x25] = "get_time";
            _func[0x26] = "jmp_if_true";
            _func[0x27] = "jmp_if_false";
            _func[0x28] = "jmp_boolean";
            _func[0x29] = "jmp";
            _func[0x2A] = "jmp_switch";
            _func[0x2B] = "load_file";
            _func[0x2C] = "load_object";
            _func[0x2D] = "load_sound";
            _func[0x2F] = "set_window_pos";
            _func[0x30] = "nop";
            _func[0x31] = "play_sound";
            _func[0x32] = "play_movie";
            _func[0x37] = "reg_query_value";
            _func[0x3C] = "ret";
            _func[0x3E] = "dump_object";
            _func[0x3F] = "get_input_state";
            _func[0x40] = "set_full_screen";
            _func[0x41] = "get_drive_path";
            _func[0x42] = "reg_set_path";
            _func[0x44] = "buf_write_byte";
            _func[0x45] = "copy_string";
            _func[0x47] = "imm_set_composition_window_pos";
            _func[0x49] = "set_cursor_pos";
            _func[0x4B] = "snd_set_volume";
            _func[0x4C] = "clear_global";
            _func[0x4D] = "shell_execute";
            _func[0x4E] = "show_window";
            _func[0x4F] = "sleep";
            _func[0x50] = "stop_sound";
            _func[0x51] = "stop_movie";
            _func[0x52] = "glb_strcat";
            _func[0x53] = "strcmp";
            _func[0x54] = "set_glb_string";
            _func[0x55] = "str_find_ch";
            _func[0x56] = "eval_str_expr";
            _func[0x57] = "strlen";
            _func[0x5A] = "get_text_width";
            _func[0x5E] = "reg_write";
            _func[0x61] = "load_dll";
            _func[0x62] = "set_game_title";
            _func[0x63] = "nop";
            _func[0x64] = "delete_file";
            _func[0x66] = "load_image";
            _func[0x67] = "nop";
            _func[0x68] = "nop";
            _func[0x69] = "nop";
            _func[0x6B] = "nop";
            _func[0x6E] = "snd_set_volume";
            _func[0x6F] = "nop";
            _func[0x70] = "nop";
            _func[0x72] = "show_dialog";
            _func[0x73] = "show_menu";
            _func[0x74] = "convert_str_case";
            _func[0x7B] = "copy_file";
            _func[0xFF] = "end";
        }

        int DByte()
        {
            _assembly.Add(Convert.ToInt32(_stream.Position), 1, Instruction.Data);
            return _reader.ReadByte();
        }

        int DWord()
        {
            _assembly.Add(Convert.ToInt32(_stream.Position), 2, Instruction.Data);
            return _reader.ReadUInt16BE();
        }

        int DAddr()
        {
            var address = Convert.ToInt32(_stream.Position);

            _assembly.Add(address, 3, Instruction.Addr);

            int val = _reader.ReadByte() << 8;
            val = (val | _reader.ReadByte()) << 8;
            val |= _reader.ReadByte();

            if (TextOutput != null)
            {
                Text(address, "#", $"addr {val:X8}");
            }

            return val;
        }

        EValue Expr()
        {
            var result_address = 0;
            object result = null;

            var textBuf = new Lazy<List<Tuple<int, string>>>();

            void TExp(int address, string text)
            {
                if (TextOutput == null)
                    return;

                textBuf.Value.Add(Tuple.Create(address, text));
            }

            while (_stream.Position < _stream.Length)
            {
                var address = Convert.ToInt32(_stream.Position);
                var code = _reader.ReadByte();

                if (code == 0xFF)
                {
                    _assembly.Add(address, 1, Instruction.ExprEnd);
                    break;
                }

                var HI = (byte)(code & 0xF0);
                var LO = (byte)(code & 0x0F);

                result_address = address;
                result = null;

                if (code < 0x40)
                {
                    if (HI == 0)
                    {
                        if (LO >= 0x08)
                        {
                            if (LO >= 0x0D)
                            {
                                if (LO == 0x0D)
                                {
                                    _assembly.Add(address, 2, Instruction.ExprLoadImmNum);
                                    result = _reader.ReadByte();
                                }
                                else if (LO == 0x0E)
                                {
                                    _assembly.Add(address, 3, Instruction.ExprLoadImmNum);
                                    result = _reader.ReadInt16BE();
                                }
                                else
                                {
                                    _assembly.Add(address, 5, Instruction.ExprLoadImmNum);
                                    result = _reader.ReadInt32BE();
                                }
                            }
                            else
                            {
                                _assembly.Add(address, 1, Instruction.ExprLoadImmNum);
                                result = 7 - LO;
                            }
                        }
                        else
                        {
                            _assembly.Add(address, 1, Instruction.ExprLoadImmNum);
                            result = LO;
                        }

                        if (TextOutput != null)
                        {
                            TExp(address, string.Format("lnum {0:X}h", result));
                        }
                    }
                    else
                    {
                        int index;

                        if (LO >= 0x0E)
                        {
                            if (LO == 0x0E)
                            {
                                _assembly.Add(address, 2, Instruction.ExprLoadNum);
                                index = _reader.ReadByte();
                            }
                            else
                            {
                                _assembly.Add(address, 3, Instruction.ExprLoadNum);
                                index = _reader.ReadInt16BE();
                            }
                        }
                        else
                        {
                            _assembly.Add(address, 1, Instruction.ExprLoadNum);
                            index = LO;
                        }

                        if (TextOutput != null)
                        {
                            TExp(address, string.Format("lnum {0}[{1}]", NumberVariableSource(HI), index));
                        }
                    }
                }
                else if (HI >= 0x80)
                {
                    if (HI == 0x80)
                    {
                        if (_unicode)
                            result = _reader.ReadUnicodeString();
                        else
                            result = _reader.ReadAnsiString();

                        var length = Convert.ToInt32(_stream.Position) - address;

                        _assembly.Add(address, length, Instruction.ExprLoadImmStr);

                        if (TextOutput != null)
                        {
                            TExp(address, string.Format("lstr \"{0}\"", result));
                        }
                    }
                    else
                    {
                        int index;

                        if (LO >= 0x0E)
                        {
                            if (LO == 0x0E)
                            {
                                _assembly.Add(address, 2, Instruction.ExprLoadNum);
                                index = _reader.ReadByte();
                            }
                            else
                            {
                                _assembly.Add(address, 3, Instruction.ExprLoadNum);
                                index = _reader.ReadInt16BE();
                            }
                        }
                        else
                        {
                            _assembly.Add(address, 1, Instruction.ExprLoadStr);
                            index = LO;
                        }

                        if (TextOutput != null)
                        {
                            TExp(address, string.Format("lstr {0}[{1}]", StringVariableSource(HI), index));
                        }
                    }
                }
                else if (HI == 0x70)
                {
                    if (LO >= 0x08)
                    {
                        // Load a value from memory and replace the value at the top of the stack
                        // Use the value at the top of the current stack as the index

                        if (LO > 0x0B)
                            _assembly.Add(address, 1, Instruction.ExprLoadStr2);
                        else
                            _assembly.Add(address, 1, Instruction.ExprLoadNum2);

                        if (TextOutput != null)
                        {
                            if (LO > 0x0B)
                                TExp(address, string.Format("lstr {0}[$R]", StringVariableSource(LO - 0x0B)));
                            else
                                TExp(address, string.Format("lnum {0}[$R]", NumberVariableSource(LO)));
                        }
                    }
                    else
                    {
                        switch (LO)
                        {
                            case 0: // neg
                                _assembly.Add(address, 1, Instruction.ExprMathNeg);
                                break;
                            case 1: // equal to zero
                                _assembly.Add(address, 1, Instruction.ExprMathEz);
                                break;
                            case 2: // rand
                                _assembly.Add(address, 1, Instruction.ExprMathRand);
                                break;
                            case 3: // sin
                                _assembly.Add(address, 1, Instruction.ExprMathSin);
                                break;
                            case 4: // cos
                                _assembly.Add(address, 1, Instruction.ExprMathCos);
                                break;
                            case 5: // atan2
                                _assembly.Add(address, 1, Instruction.ExprMathAtan2);
                                break;
                            case 6: // sqrt
                                _assembly.Add(address, 1, Instruction.ExprMathSqrt);
                                break;
                            default:
                                throw new Exception("Unknown math instruction");
                        }

                        if (TextOutput != null)
                        {
                            switch (LO)
                            {
                                case 0: // neg
                                    TExp(address, "neg");
                                    break;
                                case 1: // equal to zero
                                    TExp(address, "ez");
                                    break;
                                case 2: // rand
                                    TExp(address, "rand");
                                    break;
                                case 3: // sin
                                    TExp(address, "sin");
                                    break;
                                case 4: // cos
                                    TExp(address, "cos");
                                    break;
                                case 5: // atan2
                                    TExp(address, "atan2");
                                    break;
                                case 6: // sqrt
                                    TExp(address, "sqrt");
                                    break;
                                default:
                                    throw new Exception("Unknown math instruction");
                            }
                        }
                    }
                }
                else if (HI == 0x50)
                {
                    switch (LO)
                    {
                        case 0x00: // ==
                            _assembly.Add(address, 1, Instruction.ExprCmpEq);
                            break;
                        case 0x01: // !=
                            _assembly.Add(address, 1, Instruction.ExprCmpNe);
                            break;
                        case 0x02: // <
                            _assembly.Add(address, 1, Instruction.ExprCmpLt);
                            break;
                        case 0x03: // <=
                            _assembly.Add(address, 1, Instruction.ExprCmpLe);
                            break;
                        case 0x04: // >
                            _assembly.Add(address, 1, Instruction.ExprCmpBt);
                            break;
                        case 0x05: // >=
                            _assembly.Add(address, 1, Instruction.ExprCmpBe);
                            break;
                        default:
                            throw new Exception("Unknown compare instruction");
                    }

                    if (TextOutput != null)
                    {
                        switch (LO)
                        {
                            case 0x00: // ==
                                TExp(address, "eq");
                                break;
                            case 0x01: // !=
                                TExp(address, "ne");
                                break;
                            case 0x02: // <
                                TExp(address, "lt");
                                break;
                            case 0x03: // <=
                                TExp(address, "le");
                                break;
                            case 0x04: // >
                                TExp(address, "bt");
                                break;
                            case 0x05: // >=
                                TExp(address, "be");
                                break;
                            default:
                                throw new Exception("Unknown compare instruction");
                        }
                    }
                }
                else if (HI == 0x60)
                {
                    switch (LO)
                    {
                        case 0x00:
                            _assembly.Add(address, 1, Instruction.ExprAdd);
                            break;
                        case 0x01:
                            _assembly.Add(address, 1, Instruction.ExprSub);
                            break;
                        case 0x08:
                            _assembly.Add(address, 1, Instruction.ExprMul);
                            break;
                        case 0x09:
                            _assembly.Add(address, 1, Instruction.ExprDiv);
                            break;
                        case 0x0A:
                            _assembly.Add(address, 1, Instruction.ExprMod);
                            break;
                        case 0x0B:
                            _assembly.Add(address, 1, Instruction.ExprAnd);
                            break;
                        case 0x0C:
                            _assembly.Add(address, 1, Instruction.ExprOr);
                            break;
                        case 0x0D:
                            _assembly.Add(address, 1, Instruction.ExprLand);
                            break;
                        case 0x0E:
                            _assembly.Add(address, 1, Instruction.ExprLor);
                            break;
                        default:
                            _assembly.Add(address, 1, Instruction.ExprMov);
                            break;
                    }

                    if (TextOutput != null)
                    {
                        switch (LO)
                        {
                            case 0x00:
                                TExp(address, "add");
                                break;
                            case 0x01:
                                TExp(address, "sub");
                                break;
                            case 0x08:
                                TExp(address, "mul");
                                break;
                            case 0x09:
                                TExp(address, "div");
                                break;
                            case 0x0A:
                                TExp(address, "mod");
                                break;
                            case 0x0B:
                                TExp(address, "and");
                                break;
                            case 0x0C:
                                TExp(address, "or");
                                break;
                            case 0x0D:
                                TExp(address, "land");
                                break;
                            case 0x0E:
                                TExp(address, "lor");
                                break;
                            default:
                                TExp(address, "mov");
                                break;
                        }
                    }
                }
                else if (code.HasFlag(0x40))
                {
                    switch (LO)
                    {
                        case 0:
                            _assembly.Add(address, 1, Instruction.ExprStoreMov);
                            break;
                        case 1:
                            _assembly.Add(address, 1, Instruction.ExprStoreAdd);
                            break;
                        case 2:
                            _assembly.Add(address, 1, Instruction.ExprStoreSub);
                            break;
                        case 3:
                            _assembly.Add(address, 1, Instruction.ExprStoreMul);
                            break;
                        case 4:
                            _assembly.Add(address, 1, Instruction.ExprStoreDiv);
                            break;
                        case 5:
                            _assembly.Add(address, 1, Instruction.ExprStoreMod);
                            break;
                        case 6:
                            _assembly.Add(address, 1, Instruction.ExprStoreAnd);
                            break;
                        case 7:
                            _assembly.Add(address, 1, Instruction.ExprStoreOr);
                            break;
                        default:
                            throw new Exception("Unknown store instruction");
                    }

                    if (TextOutput != null)
                    {
                        switch (LO)
                        {
                            case 0:
                                TExp(address, "$mov");
                                break;
                            case 1:
                                TExp(address, "$add");
                                break;
                            case 2:
                                TExp(address, "$sub");
                                break;
                            case 3:
                                TExp(address, "$mul");
                                break;
                            case 4:
                                TExp(address, "$div");
                                break;
                            case 5:
                                TExp(address, "$mod");
                                break;
                            case 6:
                                TExp(address, "$and");
                                break;
                            case 7:
                                TExp(address, "$or");
                                break;
                            default:
                                throw new Exception("Unknown store instruction");
                        }
                    }
                }
                else
                {
                    throw new Exception("Unknown expression instruction");
                }
            }

            if (TextOutput != null)
            {
                var buf = textBuf.Value;

                if (buf.Count > 1)
                {
                    for (int i = 0; i < buf.Count; i++)
                    {
                        if (i == 0)
                            Text(buf[i].Item1, "┌", buf[i].Item2);
                        else if (i == buf.Count - 1)
                            Text(buf[i].Item1, "└", buf[i].Item2);
                        else
                            Text(buf[i].Item1, "├", buf[i].Item2);
                    }
                }
                else
                {
                    Text(buf[0].Item1, " ", buf[0].Item2);
                }
            }

            return new EValue(result_address, result);
        }

        EValue[] ExprList()
        {
            var result = new List<EValue>(16);

            while (_stream.Position < _stream.Length)
            {
                if (DByte() == 0)
                    break;
                result.Add(Expr());
            }

            return result.ToArray();
        }

        static string NumberVariableSource(int id)
        {
            if (id == 0x08 || id == 0x10)
                return "S:";
            if (id == 0x09 || id == 0x20)
                return "P:";
            if (id == 0x0A || id == 0x30)
                return "G:";

            throw new Exception("Bad number variable source id");
        }

        static string StringVariableSource(int id)
        {
            if (id == 0x01 || id == 0x13 || id == 0x90)
                return "S:";
            if (id == 0x02 || id == 0x11 || id == 0xA0)
                return "P:";
            if (id == 0x03 || id == 0x12 || id == 0xB0)
                return "G:";

            throw new Exception("Bad string variable source id");
        }

        void Text(int address, string arrow, string text)
        {
            if (TextOutput != null)
            {
                TextOutput.WriteLine(string.Format("{0:X8} {1} {2}", address, arrow, text));
            }
        }
    }

    class EValue
    {
        public int Address;
        public object Value;

        public bool IsNumber => Value.IsNumber();
        public bool IsString => Value.IsString();
        public int Number => Convert.ToInt32(Value);
        public string String => Convert.ToString(Value);

        public EValue(int address, object value)
        {
            Address = address;
            Value = value;
        }
    }

    enum Instruction
    {
        Data,
        Func,
        Addr,
        Nop,
        End,
        ExprLoadNum,
        ExprLoadNum2,
        ExprLoadImmNum,
        ExprLoadStr,
        ExprLoadStr2,
        ExprLoadImmStr,
        ExprMathNeg,
        ExprMathEz,
        ExprMathRand,
        ExprMathSin,
        ExprMathCos,
        ExprMathAtan2,
        ExprMathSqrt,
        ExprCmpEq,
        ExprCmpNe,
        ExprCmpLt,
        ExprCmpLe,
        ExprCmpBt,
        ExprCmpBe,
        ExprAdd,
        ExprSub,
        ExprMul,
        ExprDiv,
        ExprMod,
        ExprAnd,
        ExprOr,
        ExprLand,
        ExprLor,
        ExprMov,
        ExprStoreMov,
        ExprStoreAdd,
        ExprStoreSub,
        ExprStoreMul,
        ExprStoreDiv,
        ExprStoreMod,
        ExprStoreAnd,
        ExprStoreOr,
        ExprEnd
    }

    class Instruct
    {
        public int Address { get; }
        public int NewAddress { get; set; }
        public int Length { get; }
        public Instruction Inst { get; }

        public Instruct(int address, int length, Instruction instruction)
        {
            Address = address;
            Length = length;
            Inst = instruction;
        }
    }

    class Assembly
    {
        public List<Instruct> Instructs { get; }

        public Assembly()
        {
            Instructs = new List<Instruct>();
        }

        public void Add(int address, int length, Instruction instruction)
        {
            Instructs.Add(new Instruct(address, length, instruction));
        }

        public int BytesLength
        {
            get => Instructs.Sum(a => a.Length);
        }
    }
}
