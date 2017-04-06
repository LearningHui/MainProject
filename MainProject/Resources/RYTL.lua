
curve = {linear = 0, ease_in = 1, ease_out = 2, ease_inout = 3};
transitionType = {flipFromLeft=0, flipFromRight=1, curlUp=2, curlDown=3, slideFromLeft=4, slideFromRight=5, slideFromUp=6, slideFromDown=7};

--[[RYTL Lua Library v1.0.1Copyright 2011--]]


RYTL = {};


function RYTL:new(o)
	o = o or {};      -- create table if users does not provide one
    setmetatable(o, self);
    self.__index = self;
    return o;
end


function RYTL:get(...)

	if 1 == arg.n then
        if type(arg[1]) == "table" then
            local _elements = document:getElementsByProperty(arg[1]);
            elements = {};
            for i, c in ipairs(_elements) do
                elements[i] = Control:new{control=c};
            end
            return elements;
        end
    end
end


function RYTL:visiable(name, ...)
	for i, v in ipairs(arg) do
		local vars = document:getElementsByProperty{name=v};
		for j, c in ipairs(vars) do
            if self:needDoReverse(name) == true then
                c:setStyleByName("display", "none");
			else
				c:setStyleByName("display", "block");
			end
		end
	end
end

function RYTL:invisiable(name, ...)
	for i, v in ipairs(arg) do
		local vars = document:getElementsByProperty{name=v};
		if vars ~= nil then
			for j, c in ipairs(vars) do
				if self:needDoReverse(name) == true then
					c:setStyleByName("display", "block");
				else
					c:setStyleByName("display", "none");
				end
			end
		end
	end
end

function RYTL:enable(name, ...)
	for i, v in ipairs(arg) do
		local vars = document:getElementsByProperty{name=v};
		for j, c in ipairs(vars) do
			if self:needDoReverse(name) == true then
				c:setStyleByName("enable", "false");
			else
				c:setStyleByName("enable", "true");
			end			
		end
	end
end

function RYTL:disable(name, ...)
	for i, v in ipairs(arg) do
		local vars = document:getElementsByProperty{name=v};
		for j, c in ipairs(vars) do
			if self:needDoReverse(name) == true then
				c:setStyleByName("enable", "true");
			else
				c:setStyleByName("enable", "false");
			end
		end
	end
end

function RYTL:needDoReverse(name)
    if name then
        local var = document:getElementsByName(name);
        local tagName = var[1]:getAttribute("tagName");
        local vType = var[1]:getAttribute("type");
        if (tagName == "input" and vType == "checkbox" and var[1].getPropertyByName("checked") == "NO") then
            return true;
        else
            return false;
        end
    end
end


function RYTL:getData(key)
    return database:getData(key);
end

function RYTL:addData(key, value)
    return database:addData(key, value);
end

function RYTL:updateData(key, value)
    return database:updateData(key, value);
end

function RYTL:insertData(key, value)
    return database:insertData(key, value);
end

function RYTL:deleteData(key)
    return database:deleteData(key);
end


function RYTL:close()
    window:close();
end

function RYTL:alert(info)
    window:alert(info);
end

function RYTL:open(param)
    window:open(param);
end

function RYTL:show(param, tag, isContent)
    if isContent == true then
        window:showContent(param, tag);
    else
        local vars = document:getElementsByName(param);
        if vars and #vars > 0 then
            window:showControl(vars[1], tag);
        end
    end
end

function RYTL:hide(id)
    window:hide(id);
end


function RYTL:replace(data)
    location:replace(data);
end

function RYTL:reload(force)
    location:reload(force);
end


function RYTL:back()
    local content = history:get(-1);
    if content then
        location:replace(content,true);
    end
end

function RYTL:go(number)
    local content = history:get(number);
    if content then
        location:replace(content);
    end
end

function RYTL:add(content)
    history:add(content);
end


function RYTL:trim(string)
    return utility:trim(string);
end

function RYTL:base64(string)
    return utility:base64(string);
end

function RYTL:escapeURL(string)
    return utility:escapeURI(string);
end


function RYTL:setInterval(interval, repeats, run)
    return timer:startTimer(interval, repeats, run);
end

function RYTL:clearInterval(timerobj)
    timer:stopTimer(timerobj);
end


function RYTL:post(header, url, body, callback, parameters, synchronous)
   if synchronous==true then
       local responseData = http:postSyn(header, url, body);
       if callback==nil then
           return responseData;
       else
           local temp = {responseBody=responseData};
           if parameters and type(parameters)=="table" then
               for k,v in pairs(parameters) do 
                   temp[k] = v;
               end
           end
           callback(temp);
       end
   else
       http:postAsyn(header, url, body, callback, parameters);
   end
end

Control = {control};

function Control:new(c)
    c = c or {};      -- create table if users does not provide one
    setmetatable(c, self);
    self.__index = self;
    return c;
end

function Control:getParent()
    element = Control:new{control=self.control:getParent()};
    return element;
end

function Control:getChildren()
    local _elements = self.control:getChildren();
	elements = {};
    for i, c in ipairs(_elements) do
        elements[i] = Control:new{control=c};
    end
    return elements;
end

function Control:css(...)
    if 1 == arg.n then
        local name = arg[1];
        return self.control:getStyleByName(name);
    elseif 2 == arg.n then
        local name = arg[1];
        local value = arg[2];
        self.control:setStyleByName(name, value);
    end
end

function Control:html(content)
    self.control:setInnerHTML(content);
end

function Control:attribute(...)
	local name = arg[1];
	return self.control:getAttribute(name);
end

function Control:property(...)
	--window:alert("Control:property");
    if 1 == arg.n then
        local name = arg[1];
        return self.control:getPropertyByName(name);
    elseif 2 == arg.n then
        local name = arg[1];
        local value = arg[2];
        self.control:setPropertyByName(name, value);
    end
end

function Control:loading(start)
    if start == true then
        self.control:showLoading();
    else
        self.control:stopLoading();
    end
end

local function matrixMultiply(matrix1, matrix1)
    
    newMatrix = {m11=nil, m12=nil, m13=nil, m21=nil, m22=nil, m23=nil, m31=nil, m32=nil, m33=nil};
    
    if table.getn(matrix1)==table.getn(matrix2) then
        newMatrix.m11 = matrix1.m11*matrix2.m11 + matrix1.m12*matrix2.m21 + matrix1.m13*matrix2.m31;
        newMatrix.m12 = matrix1.m11*matrix2.m12 + matrix1.m12*matrix2.m22 + matrix1.m13*matrix2.m32;
        newMatrix.m13 = matrix1.m11*matrix2.m13 + matrix1.m12*matrix2.m23 + matrix1.m13*matrix2.m33;
        
        newMatrix.m21 = matrix1.m21*matrix2.m11 + matrix1.m22*matrix2.m21 + matrix1.m23*matrix2.m31;
        newMatrix.m22 = matrix1.m21*matrix2.m12 + matrix1.m22*matrix2.m22 + matrix1.m23*matrix2.m32;
        newMatrix.m23 = matrix1.m21*matrix2.m13 + matrix1.m22*matrix2.m23 + matrix1.m23*matrix2.m33;
        
        newMatrix.m31 = matrix1.m31*matrix2.m11 + matrix1.m32*matrix2.m21 + matrix1.m33*matrix2.m31;
        newMatrix.m32 = matrix1.m31*matrix2.m12 + matrix1.m32*matrix2.m22 + matrix1.m33*matrix2.m32;
        newMatrix.m33 = matrix1.m31*matrix2.m13 + matrix1.m32*matrix2.m23 + matrix1.m33*matrix2.m33;
        
        return newMatrix;
    end
    
end


local startAngle;
local ctimer;
local function run(parameters)

    local control = parameters[1];
    local style = parameters[2];   
    local endAngle;
    local sin;
    local cos;
    
    local matrix = control:getMatrix();
    local m11 = matrix["m11"];
    local m12 = matrix["m12"];
    local m13 = matrix["m13"];
    local m21 = matrix["m21"];
    local m22 = matrix["m22"];
    local m23 = matrix["m23"];
    local m31 = matrix["m31"];
    local m32 = matrix["m32"];
    local m33 = matrix["m33"];
    local offsetm11;
    local offsetm12;
    local offsetm13;
    local offsetm21;
    local offsetm22;
    local offsetm23;
    local offsetm31;
    local offsetm32;
    local offsetm33;
    local newm11;
    local newm12;
    local newm13;
    local newm21;
    local newm22;
    local newm23;
    local newm31;
    local newm32;
    local newm33;
    
    local modulus;
    
    
    if style~="scale" and style~="shearX" and style~="shearY" then
        endAngle = parameters[3];
    
        startAngle = startAngle + 2;
        
        if startAngle>=endAngle then
            startAngle = endAngle
        end
        
        sin = math.sin(math.rad(startAngle));
        cos = math.cos(math.rad(startAngle));
        if startAngle==0 or startAngle==180 or startAngle==360 then
            sin = 0;
        end
        if startAngle==90 or startAngle==270 then
            cos = 0;
        end
    end
    
    
    if style=="scale" then
        
        offsetm11 = parameters[3];
        newm11 = parameters[4];
        offsetm22 = parameters[5];
        newm22 = parameters[6];
        
        m11 = m11 + offsetm11;
        m22 = m22 + offsetm22;
        
        
        if m11>=newm11 then
            m11 = newm11;
        end
        if m22>=newm22 then
            m22 = newm22;
        end
        
        control:setMatrix{m11=m11,m22=m22};
        
    elseif style=="shearX" then
        
        offsetm21 = parameters[3];
        modulus = parameters[4];
        
        m21 = m21 + offsetm21;
        
        if offsetm21>0 then
            if m21>=modulus then
                m21 = modulus;
                timer:stopTimer(atimer);
                atimer = nil;
            end
        else
            if m21<=modulus then
                m21 = modulus;
                timer:stopTimer(atimer);
                atimer = nil;
            end
        end
        
        control:setMatrix{m21=m21};
        
    elseif style=="shearY" then
        offsetm12 = parameters[3];
        modulus = parameters[4];
        
        m12 = m12 + offsetm12;
        
        if offsetm12>0 then
            if m12>=modulus then
                m12 = modulus;
                timer:stopTimer(atimer);
                atimer = nil;
            end
        else
            if m12<=modulus then
                m12 = modulus;
                timer:stopTimer(atimer);
                atimer = nil;
            end
        end
        
        control:setMatrix{m12=m12};
        
    elseif style=="rotateY" then
        
        control:setMatrix{m11=cos,m13=-sin,m31=sin,m33=cos};
        
    elseif style=="rotateZClockwise" then
        
        control:setMatrix{m11=cos, m12=sin, m21=-sin, m22=cos};
        
    elseif style=="rotateZAntiClockwise" then
        
        control:setMatrix{m11=cos, m12=-sin, m21=sin, m22=cos};
        
    elseif style=="rotateX" then
        
        control:setMatrix{m22=cos, m23=sin, m32=-sin, m33=cos};
        
    end
    
    
    if style=="scale" then
        if m11==newm11 and m22==newm22 then
            timer:stopTimer(atimer);
            atimer = nil;
        end
    else
        if startAngle == endAngle then
            timer:stopTimer(atimer);
            atimer = nil;
        end
    end
    
end


function Control:scale(scaleWidthTimes, scaleHeightTimes, animationInterval)
    
    if animationInterval>0 then
        
        local matrix = self.control:getMatrix();
        local m11 = matrix["m11"];
        local m22 = matrix["m22"];
        
        local offsetm11 = (scaleWidthTimes - m11)/10.0;
        local offsetm22 = (scaleHeightTimes - m22)/10.0;
        ctimer = timer:startTimer(animationInterval, true, run, {self.control, "scale", offsetm11, scaleWidthTimes, offsetm22, scaleHeightTimes});
    else
        self.control:setMatrix{m11=scaleWidthTimes, m22=scaleHeightTimes};
    end
end


function Control:rotateZClockwise(angle, animationInterval)

    startAngle = 0.0;
    
    local sin = math.sin(math.rad(angle));
    local cos = math.cos(math.rad(angle));
    
    if degree==0 or degree==180 or degree==360 then
        sin = 0;
    end
    
    if degree==90 or degree==270 then
        cos = 0;
    end
    
    if animationInterval > 0 then
        ctimer = timer:startTimer(animationInterval, true, run, {self.control, "rotateZClockwise", angle});
    else
        self.control:setMatrix{m11=cos, m12=sin, m21=-sin, m22=cos};
    end
    
end


function Control:rotateZAntiClockwise(angle, animationInterval)

    startAngle = 0.0;
    
    local sin = math.sin(math.rad(angle));
    local cos = math.cos(math.rad(angle));
    
    if angle==0 or angle==180 or angle==360 then
        sin = 0;
    end
    
    if angle==90 or angle==270 then
        cos = 0;
    end
    
    if animationInterval>0 then
        ctimer = timer:startTimer(animationInterval, true, run, {self.control, "rotateZAntiClockwise", angle});
    else
        self.control:setMatrix{m11=cos, m12=-sin, m21=sin, m22=cos};
    end
    
end


function Control:rotateY(angle, animationInterval)

    startAngle = 0.0;
    
    local sin = math.sin(math.rad(angle));
    local cos = math.cos(math.rad(angle));
    
    if angle==0 or angle==180 or angle==360 then
        sin = 0;
    end
    
    if angle==90 or angle==270 then
        cos = 0;
    end
    
    if animationInterval>0 then
        ctimer = timer:startTimer(animationInterval, true, run, {self.control, "rotateY", angle});
    else
        self.control:setMatrix{m11=cos,m13=sin,m31=-sin,m33=cos};
    end
    
end


function Control:rotateX(angle, animationInterval)
    
    startAngle = 0.0;
    
    local sin = math.sin(math.rad(angle));
    local cos = math.cos(math.rad(angle));
    
    if angle==0 or angle==180 or angle==360 then
        sin = 0;
    end
    
    if angle==90 or angle==270 then
        cos = 0;
    end
    
    if animationInterval>0 then
        ctimer = timer:startTimer(animationInterval, true, run, {self.control, "rotateX", angle});
    else
        self.control:setMatrix{m22=cos,m23=sin,m32=-sin,m33=cos};
    end
    
end


function Control:shearX(modulus, animationInterval)

    local matrix = self.control:getMatrix();
    local m21 = matrix["m21"];
        
    local offsetm21 = (modulus - m21)/10.0;
    
    if animationInterval>0 then
        ctimer = timer:startTimer(animationInterval, true, run, {self.control, "shearX", offsetm21, modulus});
    else
        self.control:setMatrix{m21=modulus};
    end
    
end


function Control:shearY(modulus, animationInterval)

    local matrix = self.control:getMatrix();
    local m12 = matrix["m12"];
        
		
    local offsetm12 = (modulus - m12)/10.0;
    
    if animationInterval>0 then
        ctimer = timer:startTimer(animationInterval, true, run, {self.control, "shearY", offsetm12, modulus});
    else
        self.control:setMatrix{m12=modulus};
    end
    
end




