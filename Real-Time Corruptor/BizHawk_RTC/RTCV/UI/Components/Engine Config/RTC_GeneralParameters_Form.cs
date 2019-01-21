﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using RTCV.CorruptCore;
using static RTCV.UI.UI_Extensions;
using RTCV.NetCore.StaticTools;

namespace RTCV.UI
{
	public partial class RTC_GeneralParameters_Form : ComponentForm, IAutoColorize
	{
		public new void HandleMouseDown(object s, MouseEventArgs e) => base.HandleMouseDown(s, e);
		public new void HandleFormClosing(object s, FormClosingEventArgs e) => base.HandleFormClosing(s, e);

		public bool DontUpdateIntensity = false;
		public bool DontUpdateUI = false;
		public int Intensity
		{
			get => RTC_Corruptcore.Intensity;
			set
			{
				if (!DontUpdateIntensity)
					RTC_Corruptcore.Intensity = value;


				if (!DontUpdateUI)
				{
					var old = DontUpdateIntensity;
					var old2 = DontUpdateUI;
					DontUpdateIntensity = true;
					DontUpdateUI = true;

					if (nmIntensity.Value != value)
						nmIntensity.Value = value;

					if (S.GET<RTC_GlitchHarvester_Form>().nmIntensity.Value != value)
						S.GET<RTC_GlitchHarvester_Form>().nmIntensity.Value = value;

					int fx = Convert.ToInt32(Math.Sqrt(value) * 2000d);

					if (track_Intensity.Value != fx)
						track_Intensity.Value = fx;

					if (S.GET<RTC_GlitchHarvester_Form>().track_Intensity.Value != fx)
						S.GET<RTC_GlitchHarvester_Form>().track_Intensity.Value = fx;

					DontUpdateIntensity = old;
					DontUpdateUI = old2;
				}
			}
		}

		public bool DontUpdateErrorDelay = false;
		public int ErrorDelay
		{
			get => RTC_Corruptcore.ErrorDelay;
			set
			{
				if (DontUpdateErrorDelay)
					return;

				RTC_Corruptcore.ErrorDelay = Convert.ToInt32(value);

				DontUpdateErrorDelay = true;

				if (nmErrorDelay.Value != value)
					nmErrorDelay.Value = value;

				int _fx = Convert.ToInt32(Math.Sqrt(value) * 2000d);

				if (track_ErrorDelay.Value != _fx)
					track_ErrorDelay.Value = _fx;

				DontUpdateErrorDelay = false;
			}
		}

		public RTC_GeneralParameters_Form()
		{
			InitializeComponent();
		}

		private void RTC_GeneralParameters_Form_Load(object sender, EventArgs e)
		{
			cbBlastRadius.SelectedIndex = 0;
		}


		public void track_ErrorDelay_Scroll(object sender, EventArgs e)
		{
			double fx = Math.Ceiling(Math.Pow((track_ErrorDelay.Value * 0.0005d), 2));
			int _fx = Convert.ToInt32(fx);

			if (_fx != ErrorDelay)
				ErrorDelay = _fx;
		}

		public void nmErrorDelay_ValueChanged(object sender, EventArgs e)
		{
			int _fx = Convert.ToInt32(nmErrorDelay.Value);

			if (_fx != ErrorDelay)
				ErrorDelay = _fx;
		}
		
		Guid? errorDelayToken = null;
		Guid? intensityToken = null;

		private void track_ErrorDelay_MouseDown(object sender, MouseEventArgs e)
		{
		}

		private void track_ErrorDelay_MouseUp(object sender, MouseEventArgs e)
		{
			track_ErrorDelay_Scroll(sender, e);
		}


		public void nmIntensity_ValueChanged(object sender, EventArgs e)
		{
			int _fx = Convert.ToInt32(nmIntensity.Value);

			if (Intensity != _fx)
				Intensity = _fx;
		}

		private void nmIntensity_KeyDown(object sender, EventArgs e)
		{
			DontUpdateIntensity = true;
		}
		private void nmIntensity_KeyUp(object sender, EventArgs e)
		{
			DontUpdateIntensity = false;
		}


		public void track_Intensity_Scroll(object sender, EventArgs e)
		{
			DontUpdateIntensity = true;
			double fx = Math.Floor(Math.Pow((track_Intensity.Value * 0.0005d), 2));
			int _fx = Convert.ToInt32(fx);

			if (Intensity != _fx)
				Intensity = _fx;
		}

		private void track_Intensity_MouseDown(object sender, MouseEventArgs e)
		{
			DontUpdateIntensity = true;
		}

		private void track_Intensity_MouseUp(object sender, EventArgs e)
		{
			double fx = Math.Floor(Math.Pow((track_Intensity.Value * 0.0005d), 2));
			int _fx = Convert.ToInt32(fx);

			DontUpdateIntensity = false;
			DontUpdateUI = false;

			Intensity = _fx;
		}

		private void cbBlastRadius_SelectedIndexChanged(object sender, EventArgs e)
		{
			switch (cbBlastRadius.SelectedItem.ToString())
			{
				case "SPREAD":
					RTC_Corruptcore.Radius = BlastRadius.SPREAD;
					break;

				case "CHUNK":
					RTC_Corruptcore.Radius = BlastRadius.CHUNK;
					break;

				case "BURST":
					RTC_Corruptcore.Radius = BlastRadius.BURST;
					break;

				case "NORMALIZED":
					RTC_Corruptcore.Radius = BlastRadius.NORMALIZED;
					break;

				case "PROPORTIONAL":
					RTC_Corruptcore.Radius = BlastRadius.PROPORTIONAL;
					break;

				case "EVEN":
					RTC_Corruptcore.Radius = BlastRadius.EVEN;
					break;
			}
		}



		private void RTC_GeneralParameters_Form_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (e.CloseReason != CloseReason.FormOwnerClosing)
			{
				e.Cancel = true;
				this.RestoreToPreviousPanel();
				return;
			}
		}

	}
}
