/****************************************************************************/
/*  Copyright (c) 2011, ADLink Technology Inc.  All rights reserved.        */
/*                                                                          */
/*  File Name   :   PCI_8164.cs                                             */        
/*  Date        :   2011/6/10                                               */
/*  Programmer  :   Chang-Zhi Lin	                                          */
/****************************************************************************/


using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.Runtime.InteropServices;


namespace Motion_W32
{
	public class Motion
	{
		
		//Initial
	 [DllImport("8164.dll")]public static extern Int16 _8164_initial(ref System.Int16 existCards);   
   //I16 FNTYPE _8164_int_enable(I16 CardNo,HANDLE *phEvent);	 
	 [DllImport("8164.dll")]public static extern Int16 _8164_int_disable(System.Int16 CardNo);


	 
	 [DllImport("8164.dll")]public static extern Int16 _8164_get_int_status(System.Int16 AxisNo, ref System.UInt32 error_int_status, ref System.UInt32 event_int_status);
	 //I16 FNTYPE _8164_link_interrupt(I16 CardNo,void ( __stdcall *callbackAddr)(I16 IntAxisNoInCard));	 
	 [DllImport("8164.dll")]public static extern Int16 _8164_set_auto_compare(System.Int16 AxisNo, System.Int16 SelectSrc);
	 [DllImport("8164.dll")]public static extern Int16 _8164_build_compare_function(System.Int16 AxisNo, System.Double Start, System.Double End, System.Double Interval, System.Int16 Device);
	 [DllImport("8164.dll")]public static extern Int16 _8164_build_compare_table(System.Int16 AxisNo, ref System.Double TableArray, System.Int32 Size, System.Int16 Device);
	 //I16 FNTYPE _8164_delay_time_mt(I16 AxisNo,F64 miniSecond, I32 Timeout_ms, HANDLE waitEvent);
   //I16 FNTYPE _8164_int_enableA(I16 CardNo,HANDLE *phEvent);
   [DllImport("8164.dll")]public static extern Int16 _8164_int_disableA(System.Int16 CardNo);
   [DllImport("8164.dll")]public static extern Int16 _8164_map_axis_event(System.Int16 AxisNo, System.Int16 EventNo, System.UInt32 IntFactor);


   [DllImport("8164.dll")]public static extern Int16 _8164_set_security_key(System.Int16 CardNo,System.UInt16 old_secu_code, System.UInt16 New_secu_code);
   [DllImport("8164.dll")]public static extern Int16 _8164_check_security_key(System.Int16 CardNo, System.UInt16 secu_code);
   [DllImport("8164.dll")]public static extern Int16 _8164_reset_security_key(System.Int16 CardNo);
   [DllImport("8164.dll")]public static extern Int16 _8164_set_user_code(System.Int16 CardNo, ref System.UInt16 DataArr, System.UInt16 ArrSize);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_user_code(System.Int16 CardNo, ref System.UInt16 DataArr, System.UInt16 ArrSize);


   [DllImport("8164.dll")]public static extern Int16 _8164_int_control(System.UInt16 cardNo, System.UInt16 intFlag);
   [DllImport("8164.dll")]public static extern Int16 _8164_set_int_factor(System.Int16 AxisNo, System.UInt32 int_factor);
   [DllImport("8164.dll")]public static extern Int16 _8164_close();
   [DllImport("8164.dll")]public static extern Int16 _8164_get_irq_channel(System.UInt16 cardNo, ref System.UInt16 irq_no);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_base_addr(System.UInt16 cardNo, ref System.UInt16 base_addr);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_io_status(System.Int16 AxisNo, ref System.UInt16 io_sts);
   [DllImport("8164.dll")]public static extern Int16 _8164_motion_done(System.Int16 AxisNo);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_position(System.Int16 AxisNo, ref System.Double pos);
   [DllImport("8164.dll")]public static extern Int16 _8164_set_position(System.Int16 AxisNo, System.Double pos);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_command(System.Int16 AxisNo, ref System.Int32 cmd);
   [DllImport("8164.dll")]public static extern Int16 _8164_set_command(System.Int16 AxisNo, System.Int32 cmd);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_error_counter(System.Int16 AxisNo, ref System.Int16 error);
   [DllImport("8164.dll")]public static extern Int16 _8164_reset_error_counter(System.Int16 AxisNo);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_target_pos(System.Int16 AxisNo, ref System.Double pos);
   [DllImport("8164.dll")]public static extern Int16 _8164_reset_target_pos(System.Int16 AxisNo, System.Double pos);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_current_speed(System.Int16 AxisNo,  ref System.Double speed);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_rest_command(System.Int16 AxisNo, ref System.Int32 rest_command);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_latch_data(System.Int16 AxisNo, System.Int16 LatchNo, ref System.Double pos);
   [DllImport("8164.dll")]public static extern Int16 _8164_check_rdp(System.Int16 AxisNo, ref System.Int32 rdp_command);
   [DllImport("8164.dll")]public static extern Int16 _8164_set_servo(System.Int16 AxisNo, System.Int16 on_off);
   [DllImport("8164.dll")]public static extern Int16 _8164_set_pls_outmode(System.Int16 AxisNo, System.Int16 pls_outmode);
   [DllImport("8164.dll")]public static extern Int16 _8164_set_pls_iptmode(System.Int16 AxisNo, System.Int16 pls_iptmode, System.Int16 pls_logic);
   [DllImport("8164.dll")]public static extern Int16 _8164_set_feedback_src(System.Int16 AxisNo, System.Int16 Src);
   [DllImport("8164.dll")]public static extern Int16 _8164_set_general_counter(System.Int16 AxisNo, System.Int16 CntSrc, System.Double CntValue);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_general_counter(System.Int16 AxisNo, ref System.Double CntValue);
          

    // PCI-8164 Only
    [DllImport("8164.dll")]public static extern Int16 _8164_d_output(System.Int16 CardNo, System.Int16 Ch_No, System.Int16 value);
    [DllImport("8164.dll")]public static extern Int16 _8164_get_dio_status(System.Int16 CardNo, ref System.UInt16 dio_sts);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_alm(System.Int16 AxisNo, System.Int16 alm_logic, System.Int16 alm_mode);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_inp(System.Int16 AxisNo, System.Int16 inp_enable, System.Int16 inp_logic);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_ltc_logic(System.Int16 AxisNo, System.Int16 ltc_logic);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_pcs_logic(System.Int16 AxisNo, System.Int16 pcs_logic);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_erc(System.Int16 AxisNo,System.Int16 erc_logic,System.Int16 erc_on_time);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_sd(System.Int16 AxisNo, System.Int16 enable, System.Int16 sd_logic, System.Int16 sd_latch, System.Int16 sd_mode);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_el(System.Int16 AxisNo, System.Int16 el_mode);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_move_ratio(System.Int16 AxisNo, System.Double move_ratio);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_sd_pin(System.Int16 AxisNo, System.Int16 Type);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_home_config(System.Int16 AxisNo, System.Int16 home_mode, System.Int16 org_logic, System.Int16 ez_logic, System.Int16 ez_count, System.Int16 erc_out);
    [DllImport("8164.dll")]public static extern Int16 _8164_home_move(System.Int16 AxisNo, System.Double StrVel, System.Double MaxVel, System.Double Tacc);
    [DllImport("8164.dll")]public static extern Int16 _8164_pulser_vmove(System.Int16 AxisNo, System.Double SpeedLimit);
    [DllImport("8164.dll")]public static extern Int16 _8164_pulser_pmove(System.Int16 AxisNo, System.Double Dist, System.Double SpeedLimit);
    [DllImport("8164.dll")]public static extern Int16 _8164_pulser_home_move(System.Int16 AxisNo, System.Int16 HomeType, System.Double SpeedLimit);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_pulser_iptmode(System.Int16 AxisNo, System.Int16 InputMode, System.Int16 Inverse);
    [DllImport("8164.dll")]public static extern Int16 _8164_backlash_comp(System.Int16 AxisNo, System.Int16 BCompPulse, System.Int16 Mode);
    [DllImport("8164.dll")]public static extern Int16 _8164_suppress_vibration(System.Int16 AxisNo, System.UInt16 ReverseTime, System.UInt16 ForwardTime);
    
    
    
    // Pitch compensate function
    [DllImport("8164.dll")]public static extern Int16 _8164_set_pitch_compensate(System.Int16 AxisNo, System.Int16 TableSize, ref System.Int32 PTableArray, ref System.Int32 NTableArray, System.Int32 TableInterval);
    [DllImport("8164.dll")]public static extern Int16 _8164_enable_pitch_compensate(System.Int16 AxisNo, System.Int16 Enable);
    [DllImport("8164.dll")]public static extern Int16 _8164_calculate_amove_pitch_compensate(System.Int16 AxisNo, System.Double Pos, ref System.Double Result);
    [DllImport("8164.dll")]public static extern Int16 _8164_calculate_rmove_pitch_compensate(System.Int16 AxisNo, System.Double Dist, ref System.Double Result);  
    [DllImport("8164.dll")]public static extern Int16 _8164_get_pitch_table_target_pos(System.Int16 AxisNo, ref System.Double TargetPos);
    [DllImport("8164.dll")]public static extern Int16 _8164_tv_move(System.Int16 AxisNo, System.Double StrVel,  System.Double MaxVel, System.Double Tacc);
    [DllImport("8164.dll")]public static extern Int16 _8164_sv_move(System.Int16 AxisNo, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double SVacc);
    [DllImport("8164.dll")]public static extern Int16 _8164_emg_stop(System.Int16 AxisNo);
    [DllImport("8164.dll")]public static extern Int16 _8164_sd_stop(System.Int16 AxisNo, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_sd_stop_ex(System.Int16 AxisNo,  System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_v_change(System.Int16 AxisNo, System.Double NewVel, System.Double Time);
    [DllImport("8164.dll")]public static extern Int16 _8164_cmp_v_change(System.Int16 AxisNo, System.Double Res_Dist, System.Double OldVel, System.Double NewVel, System.Double Time);
    [DllImport("8164.dll")]public static extern Int16 _8164_p_change(System.Int16 AxisNo, System.Double NewPos);
    [DllImport("8164.dll")]public static extern Int16 _8164_fix_speed_range(System.Int16 AxisNo, System.Double MaxVel);
    [DllImport("8164.dll")]public static extern Int16 _8164_unfix_speed_range(System.Int16 AxisNo);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_continuous_move(System.Int16 AxisNo, System.Int16 conti_logic);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_soft_limit(System.Int16 AxisNo, System.Int32 PLimit, System.Int32 NLimit);
    [DllImport("8164.dll")]public static extern Int16 _8164_disable_soft_limit(System.Int16 AxisNo);
    [DllImport("8164.dll")]public static extern Int16 _8164_enable_soft_limit(System.Int16 AxisNo, System.Int16 Action);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_error_counter_check(System.Int16 AxisNo, System.Int16 Tolerance, System.Int16 On_Off);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_general_comparator(System.Int16 AxisNo, System.Int16 CmpSrc, System.Int16 CmpMethod, System.Int16 CmpAction, System.Double Data);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_trigger_comparator(System.Int16 AxisNo, System.Int16 CmpSrc, System.Int16 CmpMethod, System.Double Data);       
    [DllImport("8164.dll")]public static extern Int16 _8164_set_trigger_type(System.Int16 AxisNo, System.Int16 TriggerType);
    [DllImport("8164.dll")]public static extern Int16 _8164_check_compare_data(System.Int16 AxisNo, System.Int16 CompType, ref System.Double Pos);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_tr_move(System.Int16 AxisNo, System.Double Dist, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_ta_move(System.Int16 AxisNo, System.Double Pos, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sr_move(System.Int16 AxisNo, System.Double Dist, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sa_move(System.Int16 AxisNo, System.Double Pos, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec);
    
    
    
    // Speed Profile Calculation
    [DllImport("8164.dll")]public static extern Int16 _8164_get_tr_move_profile(System.Int16 AxisNo, System.Double Dist, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, ref System.Double pStrVel, ref System.Double pMaxVel, ref System.Double pTacc, ref System.Double pTdec,  ref System.Double pTconst);  
    [DllImport("8164.dll")]public static extern Int16 _8164_get_ta_move_profile(System.Int16 AxisNo, System.Double Pos, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, ref System.Double pStrVel, ref System.Double pMaxVel, ref System.Double pTacc, ref System.Double pTdec,  ref System.Double pTconst);  
    [DllImport("8164.dll")]public static extern Int16 _8164_get_sr_move_profile(System.Int16 AxisNo, System.Double Dist, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec, ref System.Double pStrVel, ref System.Double pMaxVel, ref System.Double pTacc, ref System.Double pTdec, ref System.Double pSVacc, ref System.Double pSVdec, ref System.Double pTconst);
    [DllImport("8164.dll")]public static extern Int16 _8164_get_sa_move_profile(System.Int16 AxisNo, System.Double Pos, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec, ref System.Double pStrVel, ref System.Double pMaxVel, ref System.Double pTacc, ref System.Double pTdec, ref System.Double pSVacc, ref System.Double pSVdec, ref System.Double pTconst);
    
    [DllImport("8164.dll")]public static extern Int16 _8164_start_tr_move_xy(System.Int16 CardNo, System.Double DistX, System.Double DistY, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_ta_move_xy(System.Int16 CardNo, System.Double PosX, System.Double PosY, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sr_move_xy(System.Int16 CardNo, System.Double DistX, System.Double DistY, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec); 
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sa_move_xy(System.Int16 CardNo, System.Double PosX, System.Double PosY, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec); 

    [DllImport("8164.dll")]public static extern Int16 _8164_start_tr_move_zu(System.Int16 CardNo, System.Double DistX, System.Double DistY, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_ta_move_zu(System.Int16 CardNo, System.Double PosX, System.Double PosY, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sr_move_zu(System.Int16 CardNo, System.Double DistX, System.Double DistY, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sa_move_zu(System.Int16 CardNo, System.Double PosX, System.Double PosY, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec);

    [DllImport("8164.dll")]public static extern Int16 _8164_start_a_arc_xy(System.Int16 CardNo, System.Double Cx, System.Double Cy, System.Double Ex, System.Double Ey, System.Int16 DIR, System.Double MaxVel);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_r_arc_xy(System.Int16 CardNo, System.Double OffsetCx, System.Double OffsetCy, System.Double OffsetEx, System.Double OffsetEy, System.Int16 DIR, System.Double MaxVel);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_a_arc_zu(System.Int16 CardNo, System.Double Cx, System.Double Cy, System.Double Ex, System.Double Ey, System.Int16 DIR, System.Double MaxVel);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_r_arc_zu(System.Int16 CardNo, System.Double OffsetCx, System.Double OffsetCy, System.Double OffsetEx, System.Double OffsetEy, System.Int16 DIR, System.Double MaxVel);
    
    [DllImport("8164.dll")]public static extern Int16 _8164_start_tr_line2(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double DistX, System.Double DistY, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_ta_line2(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double PosX, System.Double PosY, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sr_line2(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double DistX, System.Double DistY, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sa_line2(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double PosX, System.Double PosY, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec);

    [DllImport("8164.dll")]public static extern Int16 _8164_start_r_arc2(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double OffsetCx, System.Double OffsetCy, System.Double OffsetEx, System.Double OffsetEy, System.Int16 DIR, System.Double MaxVel);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_a_arc2(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double Cx, System.Double Cy, System.Double Ex, System.Double Ey, System.Int16 DIR, System.Double MaxVel);


    [DllImport("8164.dll")]public static extern Int16 _8164_start_tr_line3(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double DistX, System.Double DistY, System.Double DistZ, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_ta_line3(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double PosX, System.Double PosY, System.Double PosZ, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sr_line3(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double DistX, System.Double DistY, System.Double DistZ, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sa_line3(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double PosX, System.Double PosY, System.Double PosZ, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec);


    [DllImport("8164.dll")]public static extern Int16 _8164_start_tr_line4(System.Int16 CardNo, System.Double DistX, System.Double DistY, System.Double DistZ, System.Double DistU, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_ta_line4(System.Int16 CardNo, System.Double PosX, System.Double PosY, System.Double PosZ, System.Double PosU, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sr_line4(System.Int16 CardNo, System.Double DistX, System.Double DistY, System.Double DistZ, System.Double DistU, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sa_line4(System.Int16 CardNo, System.Double PosX, System.Double PosY, System.Double PosZ, System.Double PosU, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec);


    [DllImport("8164.dll")]public static extern Int16 _8164_set_tr_move_all(System.Int16 TotalAxes, ref System.Int16 AxisArray, ref System.Double DistA, ref System.Double StrVelA, ref System.Double MaxVelA, ref System.Double TaccA, ref System.Double TdecA);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_move_all(System.Int16 FirstAxisNo);
    [DllImport("8164.dll")]public static extern Int16 _8164_stop_move_all(System.Int16 FirstAxisNo);
    //I16 FNTYPE _8164_config_from_file(char *file_name);
    [DllImport("8164.dll")]public static extern Int16 _8164_config_from_file(ref string file_name);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_fa_speed(System.Int16 AxisNo, System.Double FA_Speed);
    [DllImport("8164.dll")]public static extern Int16 _8164_check_compare_status(System.Int16 AxisNo, ref System.UInt16 cmp_sts);
    [DllImport("8164.dll")]public static extern Int16 _8164_check_continuous_buffer(System.Int16 AxisNo);
    
    
    // New
    [DllImport("8164.dll")]public static extern Int16 _8164_set_sa_move_all(System.Int16 TotalAx, ref System.Int16 AxisArray, ref System.Double PosA, ref System.Double StrVelA, ref System.Double MaxVelA, ref System.Double TaccA, ref System.Double TdecA, ref System.Double SVaccA, ref System.Double SVdecA);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_ta_move_all(System.Int16 TotalAx, ref System.Int16 AxisArray, ref System.Double PosA, ref System.Double StrVelA, ref System.Double MaxVelA, ref System.Double TaccA, ref System.Double TdecA);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_sr_move_all(System.Int16 TotalAx, ref System.Int16 AxisArray, ref System.Double DistA, ref System.Double StrVelA, ref System.Double MaxVelA, ref System.Double TaccA, ref System.Double TdecA, ref System.Double SVaccA, ref System.Double SVdecA);
    
    [DllImport("8164.dll")]public static extern Int16 _8164_set_latch_source(System.Int16 AxisNo, System.Int16 ltc_src);
    [DllImport("8164.dll")]public static extern Int16 _8164_reset_counter_when_latch(System.Int16 AxisNo, System.Int16 EnableByBit);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_idle_pulse(System.Int16 AxisNo, System.Int16 idl_pulse);
    [DllImport("8164.dll")]public static extern Int16 _8164_delay_time(System.Int16 AxisNo, System.Double miniSecond);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_pulser_ratio(System.Int16 AxisNo, System.Int16 PDV, System.Int16 PMG);
    [DllImport("8164.dll")]public static extern Int16 _8164_pulser_r_line2(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double DistX, System.Double DistY, System.Double SpeedLimit);
    [DllImport("8164.dll")]public static extern Int16 _8164_pulser_r_arc2(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double OffsetCx, System.Double OffsetCy, System.Double OffsetEx, System.Double OffsetEy, System.Int16 DIR, System.Double MaxVel);
    [DllImport("8164.dll")]public static extern Int16 _8164_pulser_a_line2(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double PosX, System.Double PosY, System.Double SpeedLimit);
    [DllImport("8164.dll")]public static extern Int16 _8164_pulser_a_arc2(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double Cx, System.Double Cy, System.Double Ex, System.Double Ey, System.Int16 DIR, System.Double MaxVel);
    [DllImport("8164.dll")]public static extern Int16 _8164_reset_counter_after_home(System.Int16 AxisNo, System.Int16 EnableByBit);
    [DllImport("8164.dll")]public static extern Int16 _8164_escape_home(System.Int16 AxisNo, System.Double SrVel, System.Double MaxVel, System.Double Tacc);
    [DllImport("8164.dll")]public static extern Int16 _8164_home_search(System.Int16 AxisNo, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double ORGOffset);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_line_move_mode(System.Int16 AxisNo, System.Int16 Mode);
    [DllImport("8164.dll")]public static extern Int16 _8164_verify_speed(System.Double StrVel, System.Double MaxVel, ref System.Double minAccT, ref System.Double maxAccT, System.Double MaxSpeed);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_tr_arc_xyu(System.Int16 CardNo, System.Double OffsetCx, System.Double OffsetCy, System.Double OffsetEx, System.Double OffsetEy, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_ta_arc_xyu(System.Int16 CardNo, System.Double Cx, System.Double Cy, System.Double Ex, System.Double Ey, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sr_arc_xyu(System.Int16 CardNo, System.Double OffsetCx, System.Double OffsetCy, System.Double OffsetEx, System.Double OffsetEy, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double SVacc);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sa_arc_xyu(System.Int16 CardNo, System.Double Cx, System.Double Cy, System.Double Ex, System.Double Ey, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double SVacc);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_tr_arc_yzu(System.Int16 CardNo, System.Double OffsetCx, System.Double OffsetCy, System.Double OffsetEx, System.Double OffsetEy, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_ta_arc_yzu(System.Int16 CardNo, System.Double Cx, System.Double Cy, System.Double Ex, System.Double Ey, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sr_arc_yzu(System.Int16 CardNo, System.Double OffsetCx, System.Double OffsetCy, System.Double OffsetEx, System.Double OffsetEy, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double SVacc);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sa_arc_yzu(System.Int16 CardNo, System.Double Cx, System.Double Cy, System.Double Ex, System.Double Ey, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double SVacc);
      
    [DllImport("8164.dll")]public static extern Int16 _8164_start_tr_arc2(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double OffsetCx, System.Double OffsetCy, System.Double OffsetEx, System.Double OffsetEy, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_ta_arc2(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double Cx, System.Double Cy, System.Double Ex, System.Double Ey, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sr_arc2(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double OffsetCx, System.Double OffsetCy, System.Double OffsetEx, System.Double OffsetEy, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sa_arc2(System.Int16 CardNo, ref System.Int16 AxisArray, System.Double Cx, System.Double Cy, System.Double Ex, System.Double Ey, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec);
    
    [DllImport("8164.dll")]public static extern Int16 _8164_start_tr_arc_xy(System.Int16 CardNo, System.Double OffsetCx, System.Double OffsetCy, System.Double OffsetEx, System.Double OffsetEy, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_ta_arc_xy(System.Int16 CardNo, System.Double Cx, System.Double Cy, System.Double Ex, System.Double Ey, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_tr_arc_zu(System.Int16 CardNo, System.Double OffsetCx, System.Double OffsetCy, System.Double OffsetEx, System.Double OffsetEy, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_ta_arc_zu(System.Int16 CardNo, System.Double Cx, System.Double Cy, System.Double Ex, System.Double Ey, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec);
   
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sr_arc_xy(System.Int16 CardNo, System.Double OffsetCx, System.Double OffsetCy, System.Double OffsetEx, System.Double OffsetEy, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sa_arc_xy(System.Int16 CardNo, System.Double Cx, System.Double Cy, System.Double Ex, System.Double Ey, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sr_arc_zu(System.Int16 CardNo, System.Double OffsetCx, System.Double OffsetCy, System.Double OffsetEx, System.Double OffsetEy, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_sa_arc_zu(System.Int16 CardNo, System.Double Cx, System.Double Cy, System.Double Ex, System.Double Ey, System.Int16 DIR, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Double SVacc, System.Double SVdec);

    [DllImport("8164.dll")]public static extern Int16 _8164_set_sync_option(System.Int16 AxisNo, System.Int16 sync_stop_on,System.Int16 cstop_output_on, System.Int16 sync_option1, System.Int16 sync_option2);
    [DllImport("8164.dll")]public static extern Int16 _8164_mask_axis_stop_int(System.Int16 AxisNo, System.Int16 int_disable);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_axis_option(System.Int16 AxisNo, System.Int16 option);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_axis_stop_int(System.Int16 AxisNo, System.Int16 axis_stop_int);
    [DllImport("8164.dll")]public static extern Int16 _8164_version_info(System.Int16 CardNo, ref System.UInt16 HardwareInfo, ref System.UInt16 SoftwareInfo, ref System.UInt16 DriverInfo);
    [DllImport("8164.dll")]public static extern Int16 _8164_version_info_ex(System.Int16 CardNo, ref System.UInt16 HardwareInfo, ref System.UInt16 SoftwareInfo, ref System.UInt16 DriverInfo);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_sync_stop_mode(System.Int16 AxisNo, System.Int16 stop_mode);
    [DllImport("8164.dll")]public static extern Int16 _8164_mask_output_pulse(System.Int16 AxisNo, System.Int16 Mask);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_sync_signal_source(System.Int16 AxisNo, System.Int16 sync_axis);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_sync_signal_mode(System.Int16 AxisNo, System.Int16 mode);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_auto_rdp(System.Int16 AxisNo, System.Int16 on_off);
    [DllImport("8164.dll")]public static extern Int16 _8164_start_ta_pickplace(ref System.Int16 AxisPair2, ref System.Double Pos3, ref System.Double SVel3, ref System.Double MVel3, ref System.Double AccT3, ref System.Double StartPoint2);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_rotary_counter(System.Int16 AxisNo, System.Int16 reset_src);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_auto_compare2(System.Int16 AxisNo, System.Int16 SelectSrc);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_fifo_data(System.Int16 AxisNo, System.Int32 Data);
    [DllImport("8164.dll")]public static extern Int16 _8164_force_cmp_output(System.Int16 AxisNo);
    [DllImport("8164.dll")]public static extern Int16 _8164_dwell_move(System.Int16 AxisNo, System.Double miniSecond);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_move_para(System.Int16 AxisNo, System.Double StrVel, System.Double MaxVel, System.Double Tacc, System.Double Tdec, System.Int16 C_Type, System.Int16 M_Type, System.Double SVacc, System.Double SVdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_fast_tr_move(System.Int16 AxisNo, System.Double Dist);
    [DllImport("8164.dll")]public static extern Int16 _8164_set_erc_off_time(System.Int16 AxisNo, System.Int16 erc_off_time);
    [DllImport("8164.dll")]public static extern Int16 _8164_get_latest_speed_setting(System.Int16 AxisNo, ref System.Double StrVel, ref System.Double MaxVel);
    [DllImport("8164.dll")]public static extern Int16 _8164_get_latest_t_acc_setting(System.Int16 AxisNo, ref System.Double Tacc, ref System.Double Tdec);
    [DllImport("8164.dll")]public static extern Int16 _8164_get_latest_s_acc_setting(System.Int16 AxisNo, ref System.Double Tacc, ref System.Double Tdec);

    // PCI-8164IL Only
   [DllImport("8164.dll")]public static extern Int16 _8164_il_get_position(System.Int16 AxisNo, ref System.Double cmp_pos);
   [DllImport("8164.dll")]public static extern Int16 _8164_il_set_position(System.Int16 AxisNo, System.Double cmp_pos);
   [DllImport("8164.dll")]public static extern Int16 _8164_il_set_compare(System.Int16 AxisNo, System.Double smaller, System.Double bigger);
   [DllImport("8164.dll")]public static extern Int16 _8164_il_get_compare(System.Int16 AxisNo, ref System.Double smaller, ref System.Double bigger);
   [DllImport("8164.dll")]public static extern Int16 _8164_il_set_int(System.Int16 AxisNo, System.Int16 enable);
   [DllImport("8164.dll")]public static extern Int16 _8164_il_get_status(System.Int16 CardNo, ref System.Int16 comp_sts);
   [DllImport("8164.dll")]public static extern Int16 _8164_il_get_int_status(System.Int16 CardNo, ref System.Int16 comp_int);
   [DllImport("8164.dll")]public static extern Int16 _8164_il_set_counter_mode(System.Int16 AxisNo, System.Int16 mode);
   [DllImport("8164.dll")]public static extern Int16 _8164_il_set_interlock(System.Int16 CardNo, System.Int16 control);
   [DllImport("8164.dll")]public static extern Int16 _8164_il_set_counter_src(System.Int16 AxisNo, System.Int16 src);
   [DllImport("8164.dll")]public static extern Int16 _8164_async_avoid(System.Int16 CardNo, System.Int32 Boundary, System.Int16 cmp_mode, System.Int16 Enable_Mode);
   [DllImport("8164.dll")]public static extern Int16 _8164_aa_set_counter_mode(System.Int16 CardNo, System.Int16 axis0_mode, System.Int16 axis1_mode, System.Int16 cnt_src);
   [DllImport("8164.dll")]public static extern Int16 _8164_aa_get_boundary(System.Int16 CardNo, ref System.Double boundary);
   [DllImport("8164.dll")]public static extern Int16 _8164_aa_reset_error_value(System.Int16 CardNo, System.Double err_value);
   [DllImport("8164.dll")]public static extern Int16 _8164_aa_get_error_value(System.Int16 CardNo, ref System.Double err_value);
   [DllImport("8164.dll")]public static extern Int16 _8164_aa_release_sd_latch(System.Int16 CardNo);
   [DllImport("8164.dll")]public static extern Int16 _8164_read_register(System.Int16 AxisNo, System.UInt16 CmdNo, ref System.Int32 RegData);
        

    // Reserved Function
   [DllImport("8164.dll")]public static extern Int16 _8164_set_counter1(System.Int16 AxisNo, System.Int32 Pos);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_counter1(System.Int16 AxisNo, ref System.Int32 Pos);
   [DllImport("8164.dll")]public static extern Int16 _8164_set_counter2(System.Int16 AxisNo, System.Int32 Pos);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_counter2(System.Int16 AxisNo, ref System.Int32 Pos);
   [DllImport("8164.dll")]public static extern Int16 _8164_set_counter3(System.Int16 AxisNo, System.Int32 Pos);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_counter3(System.Int16 AxisNo, ref System.Int32 Pos);
   [DllImport("8164.dll")]public static extern Int16 _8164_set_counter4(System.Int16 AxisNo, System.Int32 Pos);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_counter4(System.Int16 AxisNo, ref System.Int32 Pos);
   
   [DllImport("8164.dll")]public static extern Int16 _8164_get_inter_status(System.Int16 AxisNo, ref System.UInt32 inter_sts);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_env(System.Int16 AxisNo, System.Int16 env_no, ref System.UInt32 env_value_h, ref System.UInt32 env_value_s);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_rmd(System.Int16 AxisNo, ref System.UInt32 rmd_value_h, ref System.UInt32 rmd_value_s);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_sts(System.Int16 AxisNo, ref System.UInt32 sts);
   [DllImport("8164.dll")]public static extern Int16 _8164_cpld_write(System.Int16 CardNo, System.UInt16 Offset, System.UInt16 Data);
   [DllImport("8164.dll")]public static extern Int16 _8164_cpld_read(System.Int16 CardNo, System.UInt16 Offset);
   [DllImport("8164.dll")]public static extern Int16 _8164_software_reset(System.Int16 CardNo);
   [DllImport("8164.dll")]public static extern Int16 _8164_preset_cmp_v_change(System.Int16 AxisNo, System.Double FromSpeed, System.Double ToSpeed, System.Double Time, System.Double RampDist);
   [DllImport("8164.dll")]public static extern Int16 _8164_set_compare_mode(System.Int16 AxisNo, System.Int16 CompNo, System.Int16 CmpSrc, System.Int16 CmpMethod, System.Int16 CmpAction);
   [DllImport("8164.dll")]public static extern Int16 _8164_set_compare_data(System.Int16 AxisNo, System.Int16 CompNo, System.Double Pos);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_sync_latch_data(System.Int16 CardNo, System.Int16 EnableBit, ref System.Double LatchPos);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_rest_arc_command(System.Int16 AxisNo, ref System.Int32 rest_command);
   [DllImport("8164.dll")]public static extern Int16 _8164_get_slow_down_point(System.Int16 AxisNo, ref System.Int32 SDP);
   [DllImport("8164.dll")]public static extern Int16 _8164_application_option(System.Int16 OptionCode);
   
	}
	
	
	
	
	
}


