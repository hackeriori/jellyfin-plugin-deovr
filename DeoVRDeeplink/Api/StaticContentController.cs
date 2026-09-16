using System.Net.Mime;
using System.Reflection;
using DeoVRDeeplink.Utilities;
using MediaBrowser.Common.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DeoVRDeeplink.Api;

[ApiController]
[Route("deovr")]
public class StaticContentController : ControllerBase
{
    private readonly ILogger<StaticContentController> _logger;
    private readonly IApplicationPaths _appPaths;
    private readonly Assembly _assembly;
    private readonly string _clientScriptResourcePath =
        $"{DeoVrDeeplinkPlugin.Instance?.GetType().Namespace}.Web.DeoVRClient.js";
    public StaticContentController(ILogger<StaticContentController> logger, IApplicationPaths appPaths)
    {
        _logger = logger;
        _appPaths = appPaths;
        _assembly = Assembly.GetExecutingAssembly();
    }
    
    /// <summary>Serves embedded client JavaScript.</summary>
    [HttpGet("ClientScript")]
    [Produces("application/javascript")]
    [AllowAnonymous]
    public IActionResult GetClientScript()
    {
        try
        {
            var stream = _assembly.GetManifestResourceStream(_clientScriptResourcePath);
            if (stream != null) return File(stream, "application/javascript");
            _logger.LogError("Resource not found: {Path}", _clientScriptResourcePath);
            return NotFound();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving client script resource.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving script resource.");
        }
    }

    /// <summary>Serves the icon image.</summary>
    [HttpGet("Icon")]
    [Produces(MediaTypeNames.Image.Png)]
    [AllowAnonymous]
    public IActionResult GetIcon()
    {
        const string resourceName = "DeoVRDeeplink.Web.Icon.png";
        try
        {
            var stream = _assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
                return NotFound();

            return File(stream, MediaTypeNames.Image.Png);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving icon resource.");
            return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving icon resource.");
        }
    }
    
    /// <summary> Return timeline images </summary>
    [HttpGet("timeline/{movieId}/4096_timelinePreview341x195.jpg")]
    [Produces(MediaTypeNames.Image.Jpeg)]
    [IpWhitelist]
    public IActionResult GetTimelineImage(string movieId)
    {
        try
        {
            if (!Guid.TryParse(movieId, out _)) return BadRequest("Invalid movie ID");
            
            var filePath = Path.Combine(_appPaths.DataPath, "deovr-timeline", $"{movieId}.jpg");
            
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("Timeline image not found: {FilePath}", filePath);
                return NotFound("Timeline image not found");
            }
            
            _logger.LogDebug("Serving timeline image: {FilePath}", filePath);
            
            return PhysicalFile(filePath, MediaTypeNames.Image.Jpeg);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving timeline image for movie {MovieId}, file {FileName}", movieId, $"{movieId}.jpg");
            return StatusCode(500, "Internal server error");
        }
    }
}
